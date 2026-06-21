"""Audit lap-joint CL150 stud OAL for all catalog DN sizes."""
from __future__ import annotations

import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "catalog_generator"))
sys.path.insert(0, str(ROOT / "catalog_generator" / "parts"))

import lj_stud_bolts  # noqa: E402
import pipe_sizes  # noqa: E402

LJ_DNS = [
    15, 20, 25, 32, 40, 50, 65, 80, 90, 100, 125, 150, 200, 250, 300, 350, 400, 450,
]


def expected_oal(dn: int) -> dict:
    row = lj_stud_bolts.lj_stud_length_mm(dn)
    grip = row["grip"]
    h = lj_stud_bolts.nut_thickness_mm(row["bolt"])
    proj = row["protrusion_mm"]
    oal_raw = grip + 2.0 * h + 2.0 * proj
    oal_ceil = int(math.ceil(oal_raw))
    return {
        **row,
        "oal_check": oal_raw,
        "L_check": oal_ceil,
        "tf": pipe_sizes.lj_ring_cl150_dims_mm(dn)["tf"],
    }


def main() -> int:
    issues: list[str] = []
    print(
        f"{'DN':>3} | {'bolt':>5} | {'n':>2} | {'tf':>5} | {'gap':>7} | "
        f"{'H':>6} | {'proj':>6} | {'OALraw':>7} | {'L':>3} | ok"
    )
    print("-" * 72)

    for dn in LJ_DNS:
        try:
            r = expected_oal(dn)
        except (ValueError, KeyError) as ex:
            issues.append(f"DN{dn}: {ex}")
            print(f"{dn:3} | ERROR: {ex}")
            continue

        h = lj_stud_bolts.nut_thickness_mm(r["bolt"])
        ok = r["L"] == r["L_check"] and abs(r["oal_raw"] - r["oal_check"]) < 0.02
        if not ok:
            issues.append(f"DN{dn}: L={r['L']} expected {r['L_check']}")

        print(
            f"{dn:3} | {r['bolt']:>5} | {r['n']:2} | {r['tf']:5.2f} | {r['grip']:7.3f} | "
            f"{h:6.3f} | {r['protrusion_mm']:6.3f} | {r['oal_raw']:7.3f} | {r['L']:3} | "
            f"{'OK' if ok else 'FAIL'}"
        )

    print(f"\nFormula: gap = T + 2×tf + 2×H;  OAL = ceil(gap + 2×H + 2×3×pitch)")
    if issues:
        print(f"ISSUES ({len(issues)}):")
        for i in issues:
            print(f"  ! {i}")
        return 1
    print(f"All {len(LJ_DNS)} LJ sizes OK.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
