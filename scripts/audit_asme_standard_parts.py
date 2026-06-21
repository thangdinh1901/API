"""Audit standard catalog parts for ASME compliance (B16.5 / B16.9 / B16.11 / B36.10).

Skips lap-joint user-data parts (STUBEND_LJ_*, LJ_RING_*, GSK_FF_*).
Reports OK/FAIL per category; exit 1 if any strict check fails.
"""
from __future__ import annotations

import ast
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "catalog_generator"))
sys.path.insert(0, str(ROOT / "catalog_generator" / "parts"))

import pipe_sizes  # noqa: E402
from STUD_BOLTS import bolting_data  # noqa: E402

PARTS = ROOT / "catalog_generator" / "parts"


def _load_dimensions(part_id: str) -> dict[int, dict]:
    """Parse DIMENSIONS dict from a CUST_*.py without importing primitives."""
    path = PARTS / part_id / part_id / f"CUST_{part_id}.py"
    tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
    for node in tree.body:
        if not isinstance(node, ast.ClassDef):
            continue
        for item in node.body:
            if isinstance(item, ast.Assign):
                for target in item.targets:
                    if isinstance(target, ast.Name) and target.id == "DIMENSIONS":
                        return ast.literal_eval(item.value)
    raise ValueError(f"DIMENSIONS not found in {path}")


WN_DIMS = _load_dimensions("WN_FLRF_CL150")
SO_DIMS = _load_dimensions("SO_FLRF_CL150")
BLD_DIMS = _load_dimensions("BLD_FLRF_CL150")

TOL_MM = 1.0
AUDIT_DN = tuple(d for d in pipe_sizes.VALID_DN if 15 <= d <= 450)

# ASME B16.5-2017 Table 7 — Class 150 raised-face flanges (mm).
# O = outside diameter; tf = minimum flange thickness C; bcd = bolt circle;
# n = bolt count; G = raised-face diameter.
ASME_B16_5_CL150_RF_MM: dict[int, dict[str, float | int]] = {
    15:  {"O": 88.9,  "tf": 9.7,  "bcd": 60.3,  "n": 4,  "G": 34.9},
    20:  {"O": 98.6,  "tf": 11.2, "bcd": 69.9,  "n": 4,  "G": 42.9},
    25:  {"O": 108.0, "tf": 12.7, "bcd": 79.2,  "n": 4,  "G": 50.8},
    32:  {"O": 117.3, "tf": 14.3, "bcd": 88.9,  "n": 4,  "G": 63.5},
    40:  {"O": 127.0, "tf": 15.9, "bcd": 98.4,  "n": 4,  "G": 73.0},
    50:  {"O": 152.4, "tf": 17.5, "bcd": 120.7, "n": 4,  "G": 92.1},
    65:  {"O": 177.8, "tf": 20.6, "bcd": 139.7, "n": 4,  "G": 104.8},
    80:  {"O": 190.5, "tf": 22.4, "bcd": 152.4, "n": 4,  "G": 127.0},
    90:  {"O": 215.9, "tf": 22.4, "bcd": 177.8, "n": 8,  "G": 139.7},
    100: {"O": 228.6, "tf": 22.4, "bcd": 190.5, "n": 8,  "G": 157.2},
    125: {"O": 254.0, "tf": 22.4, "bcd": 215.9, "n": 8,  "G": 185.7},
    150: {"O": 279.4, "tf": 23.9, "bcd": 241.3, "n": 8,  "G": 215.9},
    200: {"O": 342.9, "tf": 26.9, "bcd": 298.5, "n": 8,  "G": 269.9},
    250: {"O": 406.4, "tf": 28.4, "bcd": 362.0, "n": 12, "G": 323.8},
    300: {"O": 482.6, "tf": 30.2, "bcd": 431.8, "n": 12, "G": 381.0},
    350: {"O": 533.4, "tf": 33.3, "bcd": 476.2, "n": 12, "G": 412.8},
    400: {"O": 596.9, "tf": 35.1, "bcd": 539.8, "n": 16, "G": 469.9},
    450: {"O": 635.0, "tf": 38.1, "bcd": 577.8, "n": 16, "G": 533.4},
}

FLANGE_KEYS = ("O", "tf", "bcd", "n", "G")


def _within(actual: float | int, expected: float | int, tol: float = TOL_MM) -> bool:
    if isinstance(expected, int) and isinstance(actual, int):
        return actual == expected
    return abs(float(actual) - float(expected)) <= tol


def _audit_flange_vs_asme(name: str, dims: dict[int, dict]) -> tuple[bool, list[str]]:
    failures: list[str] = []
    for dn in AUDIT_DN:
        if dn not in dims:
            failures.append(f"DN{dn}: missing in {name} DIMENSIONS")
            continue
        ref = ASME_B16_5_CL150_RF_MM.get(dn)
        if ref is None:
            failures.append(f"DN{dn}: no ASME reference row")
            continue
        row = dims[dn]
        for key in FLANGE_KEYS:
            if key not in row:
                failures.append(f"DN{dn}: {name} missing '{key}'")
                continue
            if not _within(row[key], ref[key]):
                failures.append(
                    f"DN{dn}: {name} {key}={row[key]} vs ASME {ref[key]} "
                    f"(delta {abs(float(row[key]) - float(ref[key])):.2f} mm)"
                )
    return len(failures) == 0, failures


def _audit_wn_hub_od() -> tuple[bool, list[str]]:
    failures: list[str] = []
    for dn in AUDIT_DN:
        wn = WN_DIMS.get(dn)
        if wn is None:
            continue
        expected = pipe_sizes.OD_SCH40_MM[dn]
        actual = wn["A"]
        if not _within(actual, expected):
            failures.append(
                f"DN{dn}: WN A={actual} vs OD_SCH40_MM={expected} "
                f"(delta {abs(actual - expected):.2f} mm)"
            )
    return len(failures) == 0, failures


def _audit_bw_tables() -> tuple[bool, list[str]]:
    failures: list[str] = []
    tables = {
        "OD_SCH40_MM": pipe_sizes.OD_SCH40_MM,
        "SCH40_WALL_MM": pipe_sizes.SCH40_WALL_MM,
        "BW_ELBOW_LR90": pipe_sizes.BW_ELBOW_LR90_CENTER_TO_FACE_MM,
        "BW_ELBOW_LR45": pipe_sizes.BW_ELBOW_LR45_CENTER_TO_FACE_MM,
        "BW_TEE_EQUAL": pipe_sizes.BW_TEE_EQUAL_CENTER_TO_END_MM,
    }
    for dn in pipe_sizes.VALID_DN:
        for label, table in tables.items():
            if dn not in table:
                failures.append(f"DN{dn}: missing from pipe_sizes.{label}")

    sr90_dns = set(pipe_sizes.VALID_SR90_DN)
    for dn in pipe_sizes.VALID_DN:
        if dn >= 25 and dn not in sr90_dns:
            failures.append(f"DN{dn}: missing SR90 elbow table (NPS 1+)")
        if dn in (15, 20) and dn in sr90_dns:
            failures.append(f"DN{dn}: SR90 present but NPS < 1")

    for dn in pipe_sizes.VALID_DN:
        if dn < 20:
            continue
        reducers = [b for a, b in pipe_sizes.BW_REDUCER_END_TO_END_MM if a == dn]
        if not reducers:
            failures.append(f"DN{dn}: no BW reducer (large) entries")
        tees = [b for a, b in pipe_sizes.BW_TEE_REDUCE_CENTER_TO_END_MM if a == dn]
        if not tees:
            failures.append(f"DN{dn}: no BW reducing-tee entries")

    return len(failures) == 0, failures


def _audit_sw_tables() -> tuple[bool, list[str]]:
    failures: list[str] = []
    tables = {
        "SW_CL3000_CENTER_TO_SOCKET": pipe_sizes.SW_CL3000_CENTER_TO_SOCKET_MM,
        "SW_CL3000_ELBOW_45": pipe_sizes.SW_CL3000_ELBOW_45_CENTER_TO_SOCKET_MM,
        "SW_CL3000_SOCKET_BORE": pipe_sizes.SW_CL3000_SOCKET_BORE_MM,
        "SW_CL3000_SOCKET_DEPTH": pipe_sizes.SW_CL3000_SOCKET_DEPTH_MM,
        "SW_CL3000_SOCKET_WALL": pipe_sizes.SW_CL3000_SOCKET_WALL_MM,
        "SW_CL3000_BORE": pipe_sizes.SW_CL3000_BORE_MM,
        "SW_PIPE_OD": pipe_sizes.SW_PIPE_OD_MM,
    }
    for dn in pipe_sizes.VALID_SW_CL3000_DN:
        for label, table in tables.items():
            if dn not in table:
                failures.append(f"DN{dn}: missing from pipe_sizes.{label}")

    for dn in pipe_sizes.VALID_SW_CL3000_DN:
        if dn >= 20:
            branches = [b for a, b in pipe_sizes.SW_TEE_REDUCE_PAIRS if a == dn]
            if not branches:
                failures.append(f"DN{dn}: no SW reducing-tee branch entries")

    return len(failures) == 0, failures


def _audit_stud_bolting() -> tuple[bool, list[str]]:
    failures: list[str] = []
    for dn in AUDIT_DN:
        nps = pipe_sizes.dn_to_nps(dn)
        try:
            bolt = bolting_data.lookup(150, dn=dn, face=bolting_data.FaceType.RF)
        except (ValueError, KeyError) as ex:
            failures.append(f"DN{dn} (NPS {nps}): bolting lookup failed — {ex}")
            continue

        if bolt["L"] is None or bolt["L"] <= 0:
            failures.append(f"DN{dn}: RF stud length missing or non-positive")

        for flange_name, dims in (
            ("WN", WN_DIMS),
            ("SO", SO_DIMS),
            ("BLD", BLD_DIMS),
        ):
            fd = dims.get(dn)
            if fd is None:
                failures.append(f"DN{dn}: missing {flange_name} row for bolting cross-check")
                continue
            if bolt["n"] != fd["n"]:
                failures.append(
                    f"DN{dn}: bolting n={bolt['n']} != {flange_name} n={fd['n']}"
                )

        if nps not in bolting_data.CLASS_150:
            failures.append(f"DN{dn}: NPS {nps} not in CLASS_150 table")

    return len(failures) == 0, failures


def _audit_gsk_rf() -> tuple[bool, list[str]]:
    """GSK_RF uses WN flange dims for G, B, bcd, n."""
    failures: list[str] = []
    keys = ("G", "B", "bcd", "n")
    for dn in AUDIT_DN:
        wn = WN_DIMS.get(dn)
        if wn is None:
            failures.append(f"DN{dn}: WN dims missing (GSK_RF source)")
            continue
        for key in keys:
            if key not in wn:
                failures.append(f"DN{dn}: WN missing '{key}' for GSK_RF")
    return len(failures) == 0, failures


def _check_dn20_lap_g() -> tuple[str, list[str]]:
    """Informational: DN20 stubend lap G vs WN RF G (LJ excluded from strict ASME)."""
    notes: list[str] = []
    dn = 20
    if dn not in pipe_sizes.STUBEND_LJ_A_MM:
        return "SKIP", ["DN20: no STUBEND_LJ_A_MM row"]
    stub_g = float(pipe_sizes.STUBEND_LJ_A_MM[dn]["G"])
    wn_g = float(WN_DIMS[dn]["G"])
    asme_g = float(ASME_B16_5_CL150_RF_MM[dn]["G"])
    if abs(stub_g - wn_g) > TOL_MM:
        notes.append(
            f"DN20 lap stub G={stub_g} vs WN/ASME G={wn_g} "
            f"(delta {abs(stub_g - wn_g):.1f} mm) - known user-table issue"
        )
    if abs(stub_g - asme_g) > TOL_MM:
        notes.append(f"DN20 lap stub G={stub_g} vs ASME G={asme_g}")
    status = "NOTED" if notes else "OK"
    return status, notes


def _print_category(label: str, ok: bool, failures: list[str]) -> None:
    tag = "OK" if ok else "FAIL"
    print(f"\n[{tag}] {label}")
    if failures:
        for f in failures:
            print(f"  ! {f}")
    else:
        print("  (no issues)")


def main() -> int:
    print("ASME standard-parts audit")
    print(f"DN range: {AUDIT_DN[0]}-{AUDIT_DN[-1]}  tolerance: {TOL_MM} mm")
    print("Skipped: STUBEND_LJ_*, LJ_RING_*, GSK_FF_* (user-provided lap-joint data)")

    categories: list[tuple[str, bool, list[str]]] = []

    ok, fails = _audit_flange_vs_asme("WN", WN_DIMS)
    categories.append(("WN_FLRF_CL150 vs ASME B16.5-2017 CL150 RF", ok, fails))

    ok, fails = _audit_flange_vs_asme("SO", SO_DIMS)
    categories.append(("SO_FLRF_CL150 vs ASME B16.5-2017 CL150 RF", ok, fails))

    ok, fails = _audit_flange_vs_asme("BLD", BLD_DIMS)
    categories.append(("BLD_FLRF_CL150 vs ASME B16.5-2017 CL150 RF", ok, fails))

    ok, fails = _audit_wn_hub_od()
    categories.append(("WN hub OD (A) vs B36.10 OD_SCH40_MM", ok, fails))

    ok, fails = _audit_bw_tables()
    categories.append(("pipe_sizes BW tables self-consistency", ok, fails))

    ok, fails = _audit_sw_tables()
    categories.append(("pipe_sizes SW tables self-consistency", ok, fails))

    ok, fails = _audit_stud_bolting()
    categories.append(("STUD_BOLTS bolting_data vs flanges", ok, fails))

    ok, fails = _audit_gsk_rf()
    categories.append(("GSK_RF source dims (WN)", ok, fails))

    lap_status, lap_notes = _check_dn20_lap_g()
    print(f"\n[INFO] DN20 lap-joint stub G check (non-blocking): {lap_status}")
    for n in lap_notes:
        print(f"  ~ {n}")

    any_fail = False
    print("\n" + "=" * 60)
    print("SUMMARY")
    print("=" * 60)
    for label, ok, fails in categories:
        _print_category(label, ok, fails)
        if not ok:
            any_fail = True

    total_fail = sum(1 for _, ok, _ in categories if not ok)
    print(f"\nCategories: {len(categories) - total_fail} OK, {total_fail} FAIL")
    if any_fail:
        print("Result: FAIL - fix issues above.")
        return 1
    print("Result: OK - all standard ASME checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
