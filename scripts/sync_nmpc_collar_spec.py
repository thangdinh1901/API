"""Sync Collar rows into Plant spec (.pspc) from long-pattern StubEnd entries.

CollarLapped joint (DefaultConnectorsConfig.xml) requires PartType=Collar in the spec.
StubLapped uses StubEnd — both are needed for lap-joint assemblies.
"""
from __future__ import annotations

import argparse
import shutil
import sqlite3
import uuid
from pathlib import Path

STUB_LONG_DESC = "STUB-END FOR LAP FLANGE"


def _new_guid_blob() -> bytes:
    return uuid.uuid4().bytes


def _max_pnpid(conn: sqlite3.Connection) -> int:
    row = conn.execute("SELECT MAX(PnPID) FROM PnPBase").fetchone()
    return int(row[0] or 0)


def sync_collar_from_stub(pspec_path: Path, *, backup: bool = True) -> int:
    if not pspec_path.is_file():
        raise SystemExit(f"Spec not found: {pspec_path}")

    if backup:
        bak = pspec_path.with_suffix(pspec_path.suffix + ".bak")
        shutil.copy2(pspec_path, bak)
        print(f"Backup: {bak}")

    conn = sqlite3.connect(pspec_path)
    conn.row_factory = sqlite3.Row

    existing_collar_nd = {
        float(r[0])
        for r in conn.execute(
            """
            SELECT e.NominalDiameter
            FROM Collar c
            JOIN EngineeringItems e ON e.PnPID = c.PnPID
            """
        )
    }

    stubs = conn.execute(
        """
        SELECT e.PnPID AS stub_pid, e.*, s.FlangeOffset, s.Shop_Field,
               b.PnPClassName, b.PnPStatus, b.PnPRevision
        FROM StubEnd s
        JOIN EngineeringItems e ON e.PnPID = s.PnPID
        JOIN PnPBase b ON b.PnPID = e.PnPID
        WHERE e.ShortDescription = ?
        ORDER BY e.NominalDiameter
        """,
        (STUB_LONG_DESC,),
    ).fetchall()

    ei_cols = [r[1] for r in conn.execute("PRAGMA table_info(EngineeringItems)")]
    base_cols = [r[1] for r in conn.execute("PRAGMA table_info(PnPBase)")]

    added = 0
    next_pid = _max_pnpid(conn) + 1

    for stub in stubs:
        nd = float(stub["NominalDiameter"])
        if nd in existing_collar_nd:
            continue

        new_pid = next_pid
        next_pid += 1

        conn.execute(
            """
            INSERT INTO PnPBase (PnPID, PnPClassName, PnPStatus, PnPRevision, PnPGuid, PnPTimestamp)
            VALUES (?, 'Collar', ?, ?, ?, ?)
            """,
            (
                new_pid,
                stub["PnPStatus"],
                stub["PnPRevision"],
                _new_guid_blob(),
                int(stub["PnPID"]) + 1_000_000,
            ),
        )

        ei_values: dict[str, object] = {col: stub[col] for col in ei_cols if col in stub.keys()}
        ei_values["PnPID"] = new_pid
        ei_values["ShortDescription"] = "Collar"
        ei_values["PartSizeLongDesc"] = str(ei_values.get("PartSizeLongDesc", "")).replace(
            "STUB-END FOR LAP FLANGE", "Collar", 1
        )
        ei_values["ContentIsoSymbolDefinition"] = "SKEY=FLSE,TYPE=LAPJOINT-STUB-END"
        if ei_values.get("ContentGeometryTemplate", "").startswith("CUST_STUBEND"):
            ei_values["ContentGeometryTemplate"] = ei_values["ContentGeometryTemplate"].replace(
                "STUBEND", "COLLAR", 1
            )

        placeholders = ", ".join("?" for _ in ei_cols)
        col_list = ", ".join(f"[{c}]" for c in ei_cols)
        conn.execute(
            f"INSERT INTO EngineeringItems ({col_list}) VALUES ({placeholders})",
            [ei_values.get(c) for c in ei_cols],
        )

        conn.execute("INSERT INTO Fasteners (PnPID) VALUES (?)", (new_pid,))
        conn.execute(
            "INSERT INTO Collar (PnPID, FlangeOffset, Shop_Field) VALUES (?, ?, ?)",
            (new_pid, stub["FlangeOffset"], stub["Shop_Field"]),
        )

        existing_collar_nd.add(nd)
        added += 1
        print(f"  DN{nd:g}: Collar PnPID {new_pid} (from stub {stub['stub_pid']})")

    conn.commit()
    conn.close()
    print(f"Added {added} Collar row(s) to {pspec_path}")
    return added


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "pspec",
        nargs="?",
        default=r"D:\04. Projects\06. NUI\NUI\Spec Sheets\NPMC.pspc",
        help="Path to .pspc spec database",
    )
    parser.add_argument("--no-backup", action="store_true")
    args = parser.parse_args()
    sync_collar_from_stub(Path(args.pspec), backup=not args.no_backup)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
