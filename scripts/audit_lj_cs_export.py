"""Compare C# CatalogFlangeBoltingCatalog LJ lengths vs lj_stud_bolts.py."""
from __future__ import annotations

import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "catalog_generator"))
sys.path.insert(0, str(ROOT / "catalog_generator" / "parts"))

import lj_stud_bolts
import pipe_sizes

NUT = {
    "1/2": 12.303,
    "5/8": 15.478,
    "3/4": 18.653,
    "7/8": 21.828,
    "1": 25.003,
    "1-1/8": 28.178,
}
PITCH = {
    "1/2": 1.954,
    "5/8": 2.309,
    "3/4": 2.540,
    "7/8": 2.822,
    "1": 3.175,
    "1-1/8": 3.175,
}
INSET = {b: NUT[b] + 3.0 * PITCH[b] for b in NUT}
RF_BOLT = {
    15: "1/2", 20: "1/2", 25: "1/2", 32: "1/2", 40: "1/2",
    50: "5/8", 65: "5/8", 80: "5/8", 90: "5/8", 100: "5/8",
    125: "5/8", 150: "3/4", 200: "3/4", 250: "7/8", 300: "7/8",
    350: "1", 400: "1", 450: "1-1/8",
}


def cs_lj_mm(dn: int) -> int:
    bolt = RF_BOLT[dn]
    ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
    h = NUT[bolt]
    grip = 1.5 + 2.0 * ring["tf"] + 2.0 * h
    inset = INSET[bolt]
    return math.ceil(grip + 2.0 * inset)


def main() -> int:
    issues = []
    print(f"{'DN':>4} | {'py':>4} | {'cs':>4} | diff")
    for dn in sorted(RF_BOLT):
        py = lj_stud_bolts.lj_stud_length_mm(dn)["L"]
        cs = cs_lj_mm(dn)
        diff = py - cs
        mark = "  " if py == cs else " !"
        print(f"{dn:4} | {py:4} | {cs:4} | {diff:+4}{mark}")
        if py != cs:
            issues.append(f"DN{dn}: py={py} cs={cs}")
    if issues:
        print("MISMATCH:")
        for i in issues:
            print(" ", i)
        return 1
    print(f"All {len(RF_BOLT)} DN: C# export matches lj_stud_bolts.py")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
