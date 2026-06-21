"""Audit NPMC spec + CATA catalog for flange insertion blockers."""
from __future__ import annotations

import sqlite3
from pathlib import Path

PSPEC = Path(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\NPMC.pspc")
PCAT = Path(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat")


def audit_spec(conn: sqlite3.Connection) -> None:
    print("=== NPMC SPEC — flange-related classes ===")
    for cls in ("Flange", "StubEnd", "Collar", "Gasket", "BoltSet", "BlindFlange"):
        try:
            n = conn.execute(f"SELECT COUNT(*) FROM [{cls}]").fetchone()[0]
            print(f"  {cls}: {n}")
        except sqlite3.OperationalError:
            print(f"  {cls}: (no table)")

    print("\n=== Flange ShortDescription x ND coverage ===")
    rows = conn.execute(
        """
        SELECT e.ShortDescription, COUNT(DISTINCT e.NominalDiameter) AS nds,
               MIN(e.NominalDiameter) AS min_nd, MAX(e.NominalDiameter) AS max_nd
        FROM Flange f
        JOIN EngineeringItems e ON e.PnPID = f.PnPID
        GROUP BY e.ShortDescription
        ORDER BY e.ShortDescription
        """
    ).fetchall()
    for r in rows:
        print(f"  {r[0]}: {r[1]} sizes ({r[2]:g}–{r[3]:g})")

    print("\n=== DN50 flange types in spec ===")
    for row in conn.execute(
        """
        SELECT b.PnPClassName, e.ShortDescription, e.NominalDiameter, e.EndType, e.PortName,
               e.PressureClass, e.Facing, e.ContentGeometryTemplate
        FROM EngineeringItems e
        JOIN PnPBase b ON b.PnPID = e.PnPID
        LEFT JOIN Flange fl ON fl.PnPID = e.PnPID
        WHERE e.NominalDiameter = 50
          AND (fl.PnPID IS NOT NULL OR e.ShortDescription LIKE '%FLANGE%')
        ORDER BY e.ShortDescription
        """
    ):
        print(" ", row)

    print("\n=== Spec rows missing PressureClass on FL port (Flange) ===")
    for row in conn.execute(
        """
        SELECT e.ShortDescription, e.NominalDiameter, e.EndType, e.PressureClass, e.PortName
        FROM Flange f
        JOIN EngineeringItems e ON e.PnPID = f.PnPID
        WHERE e.EndType = 'FL' AND (e.PressureClass IS NULL OR e.PressureClass = '')
        LIMIT 15
        """
    ):
        print(" ", row)

    print("\n=== Spec gasket/stud at DN50 ===")
    for row in conn.execute(
        """
        SELECT b.PnPClassName, e.ShortDescription, e.NominalDiameter
        FROM EngineeringItems e
        JOIN PnPBase b ON b.PnPID = e.PnPID
        WHERE e.NominalDiameter = 50
          AND b.PnPClassName IN ('Gasket', 'BoltSet')
        """
    ):
        print(" ", row)


def audit_pcat(conn: sqlite3.Connection) -> None:
    print("\n=== CATA PCAT — flange families ===")
    for row in conn.execute(
        """
        SELECT e.ShortDescription, COUNT(*) AS n
        FROM Flange f
        JOIN EngineeringItems e ON e.PnPID = f.PnPID
        GROUP BY e.ShortDescription
        """
    ):
        print(f"  {row[0]}: {row[1]}")

    print("\n=== DN50 flanges — ports via PartPort ===")
    for row in conn.execute(
        """
        SELECT e.ShortDescription, pp.Name, pt.EndType, pt.PressureClass, pt.Facing
        FROM EngineeringItems e
        JOIN Flange fl ON fl.PnPID = e.PnPID
        JOIN PartPort pp ON pp.Part = e.PnPID
        JOIN Port pt ON pt.PnPID = pp.Port
        WHERE e.NominalDiameter = 50
        ORDER BY e.ShortDescription, pp.Name
        """
    ):
        print(" ", row)

    print("\n=== Flanges with NO PartPort rows ===")
    n = conn.execute(
        """
        SELECT COUNT(*)
        FROM EngineeringItems e
        JOIN Flange fl ON fl.PnPID = e.PnPID
        LEFT JOIN PartPort pp ON pp.Part = e.PnPID
        WHERE pp.Part IS NULL
        """
    ).fetchone()[0]
    print(f"  count: {n}")


def compare_dn50_flange_spec_vs_pcat() -> None:
    print("\n=== DN50 flange types: spec vs catalog ===")
    sc = sqlite3.connect(PSPEC)
    cc = sqlite3.connect(PCAT)
    spec_types = {
        r[0]
        for r in sc.execute(
            """
            SELECT DISTINCT e.ShortDescription
            FROM Flange f JOIN EngineeringItems e ON e.PnPID = f.PnPID
            WHERE e.NominalDiameter = 50
            """
        )
    }
    pcat_types = {
        r[0]
        for r in cc.execute(
            """
            SELECT DISTINCT e.ShortDescription
            FROM Flange f JOIN EngineeringItems e ON e.PnPID = f.PnPID
            WHERE e.NominalDiameter = 50
            """
        )
    }
    print("  in spec only:", sorted(spec_types - pcat_types) or "(none)")
    print("  in catalog only:", sorted(pcat_types - spec_types) or "(none)")
    print("  both:", sorted(spec_types & pcat_types))
    sc.close()
    cc.close()


def main() -> None:
    if PSPEC.is_file():
        c = sqlite3.connect(PSPEC)
        audit_spec(c)
        c.close()
    else:
        print("Missing pspec:", PSPEC)

    if PCAT.is_file():
        c = sqlite3.connect(PCAT)
        audit_pcat(c)
        c.close()
    else:
        print("Missing pcat:", PCAT)

    compare_dn50_flange_spec_vs_pcat()


if __name__ == "__main__":
    main()
