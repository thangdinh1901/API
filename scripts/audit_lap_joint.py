"""Audit lap joint catalog — all DN sizes, Python/C# table sync, port metadata."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "catalog_generator"))
sys.path.insert(0, str(ROOT / "catalog_generator" / "parts"))

import lj_stud_bolts  # noqa: E402
import pipe_sizes  # noqa: E402

CS_STUB = ROOT / "Plant3DCatalogComposer/Services/CatalogStubEndTable.cs"
CS_RING = ROOT / "Plant3DCatalogComposer/Services/CatalogLjRingCl150Table.cs"
PIPE_DNS = {15, 20, 25, 32, 40, 50, 65, 80, 90, 100, 125, 150, 200, 250, 300, 350, 400, 450}


def parse_cs_stub() -> dict[int, tuple]:
    text = CS_STUB.read_text(encoding="utf-8")
    out = {}
    for m in re.finditer(
        r"\[(\d+)\]\s*=\s*\(([\d.]+),\s*([\d.]+),\s*([\d.]+),\s*([\d.]+),\s*([\d.]+)\)",
        text,
    ):
        dn = int(m.group(1))
        out[dn] = tuple(float(m.group(i)) for i in range(2, 7))
    return out


def parse_cs_ring() -> dict[int, tuple]:
    text = CS_RING.read_text(encoding="utf-8")
    out = {}
    for m in re.finditer(
        r"\[(\d+)\]\s*=\s*\(([\d.]+),\s*([\d.]+),\s*([\d.]+),\s*([\d.]+)\)",
        text,
    ):
        dn = int(m.group(1))
        out[dn] = tuple(float(m.group(i)) for i in range(2, 6))
    return out


def main() -> int:
    cs_stub = parse_cs_stub()
    cs_ring = parse_cs_ring()
    blockers: list[str] = []
    warnings: list[str] = []

    for dn, row in pipe_sizes.STUBEND_LJ_A_MM.items():
        py = (row["do"], row["F_sh"], row["F_lg"], row["G"], row["stub_thk"])
        if dn not in cs_stub:
            blockers.append(f"DN{dn}: missing in CatalogStubEndTable.cs")
        elif cs_stub[dn] != py:
            blockers.append(f"DN{dn}: C# stub table != pipe_sizes.py")

    for dn in pipe_sizes.LJ_RING_CL150_PLANT_MM:
        d = pipe_sizes.lj_ring_cl150_dims_mm(dn)
        py = (d["L"], d["O"], d["D2"], d["tf"])
        if dn not in cs_ring:
            blockers.append(f"DN{dn}: missing in CatalogLjRingCl150Table.cs")
        elif cs_ring[dn] != py:
            blockers.append(f"DN{dn}: C# ring table != lj_ring_cl150_dims_mm()")

    for dn in sorted(PIPE_DNS):
        if dn not in pipe_sizes.STUBEND_LJ_A_MM:
            blockers.append(f"DN{dn}: no stub row")
        if dn not in pipe_sizes.LJ_RING_CL150_PLANT_MM:
            blockers.append(f"DN{dn}: no backing ring row")

    for dn in sorted(cs_stub.keys() - cs_ring.keys()):
        warnings.append(f"DN{dn}: stub exported but no backing ring (stub-only)")

    print(f"{'DN':>4} | {'do':>5} | {'lapB':>5} | {'lapG':>5} | {'ringTf':>6} | {'engagement':>10} | {'matchOD':>7} | notes")
    print("-" * 72)

    for dn in sorted(PIPE_DNS):
        st = pipe_sizes.stubend_lj_a_dims_mm(dn, "long")
        ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
        notes: list[str] = []
        if st["G"] >= ring["O"]:
            blockers.append(f"DN{dn}: lap G >= ring OD")
            notes.append("G>=O")
        if ring["model_bore"] >= st["G"]:
            blockers.append(f"DN{dn}: ring bore >= lap G (collar cannot seat on shoulder)")
            notes.append("bore>=G")
        if abs(st["T"] - ring["stub_lap_t"]) > 0.01:
            blockers.append(f"DN{dn}: lap T != ring stub_lap_t")
            notes.append("T mismatch")
        weld_do = st["OD"]
        print(
            f"{dn:4} | {weld_do:5.0f} | {st['T']:5.2f} | {st['G']:5.0f} | {ring['tf']:6.1f} | "
            f"{st['T']:10.2f} | {weld_do:7.0f} | {', '.join(notes) or 'OK'}"
        )

    print(f"\nStub sizes: {len(cs_stub)}  Ring sizes: {len(cs_ring)}  Joint set: {len(PIPE_DNS)}")
    print(f"BLOCKERS ({len(blockers)}):")
    for b in blockers or ["none"]:
        print(f"  ! {b}")
    print(f"WARNINGS ({len(warnings)}):")
    for w in warnings or ["none"]:
        print(f"  ? {w}")

    print("Export rules (all DN):")
    print("  ContentGeometryParamDefinition: DN / DN,T / DN,DN2 / DN,CEL (names only); values in DN/T/... columns")
    print("  Stub:  LAP port @ shoulder x=B; FlangeOffset=B; OF=-1 (native CPMUW)")
    print("  Ring:  LAP port @ stub lap B; catalog L = overall ring depth only")
    print("  Run:   python scripts/sync_lap_joint_cs_tables.py  after editing pipe_sizes.py")

    return 1 if blockers else 0


if __name__ == "__main__":
    raise SystemExit(main())
