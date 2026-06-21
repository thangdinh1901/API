"""Audit lap-joint CL150 part placement — axial envelopes and overlap checks.

Joint +X (east). x=0 = west gasket FF / west ring FL / stub lap face.
West ring solid x in [-L, 0] (pipe at -X). East ring x in [T, T+L].
"""
from __future__ import annotations

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
GASKET_T = 1.5


def joint_envelope(dn: int) -> dict:
    st = pipe_sizes.stubend_lj_a_dims_mm(dn, "long")
    ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
    bolt = lj_stud_bolts.lj_stud_length_mm(dn, gasket_t=GASKET_T)
    b = float(st["T"])
    f = float(st["F"])
    tf = float(ring["tf"])
    rl = float(ring["L"])
    grip = float(bolt["grip"])
    h = lj_stud_bolts.nut_thickness_mm(bolt["bolt"])
    inset = lj_stud_bolts.stud_bearing_inset_mm(bolt["bolt"])
    proj = bolt["protrusion_mm"]

    x_west, x_east, _ = lj_stud_bolts.lj_joint_nut_bearings_mm(
        dn, bolt["bolt"], gasket_t=GASKET_T
    )
    x_stud = lj_stud_bolts.lj_stud_start_x_west(x_west, bolt["bolt"])
    oal_vis = float(bolt["oal_raw"])
    west_inner = x_stud + inset
    east_inner = x_stud + oal_vis - inset
    west_nut = (x_stud + proj, x_stud + proj + h)
    east_nut = (x_stud + oal_vis - proj - h, x_stud + oal_vis - proj)

    return {
        "dn": dn,
        "B": b,
        "F": f,
        "G": float(st["G"]),
        "tf": tf,
        "ring_L": rl,
        "bore": float(ring["model_bore"]),
        "pipe_od": float(st["OD"]),
        "gasket": (0.0, GASKET_T),
        "west_ring": (-rl, 0.0),
        "west_plate": (-tf, 0.0),
        "west_stub_lap": (-b, 0.0),
        "west_stub_barrel": (-f, -b),
        "east_ring": (GASKET_T, GASKET_T + rl),
        "east_plate": (GASKET_T, GASKET_T + tf),
        "east_stub_lap": (GASKET_T, GASKET_T + b),
        "east_stub_barrel": (GASKET_T + b, GASKET_T + f),
        "x_west_bearing": x_west,
        "x_east_bearing": x_east,
        "west_nut": west_nut,
        "east_nut": east_nut,
        "west_inner": west_inner,
        "east_inner": east_inner,
        "lap_port": b,
        "ring_lap_x": b,
        "bolt_L": bolt["L"],
        "grip": grip,
    }


def _overlap(a: tuple[float, float], b: tuple[float, float]) -> float:
    lo = max(a[0], b[0])
    hi = min(a[1], b[1])
    return max(0.0, hi - lo)


def check(dn: int) -> list[str]:
    e = joint_envelope(dn)
    issues: list[str] = []

    if abs(e["lap_port"] - e["ring_lap_x"]) > 0.01:
        issues.append(f"LAP port {e['lap_port']:.2f} != ring LAP {e['ring_lap_x']:.2f}")

    if e["pipe_od"] >= e["bore"] - 0.5:
        issues.append(f"pipe OD {e['pipe_od']:.1f} >= ring bore {e['bore']:.1f}")

    if e["bore"] >= e["G"] - 0.5:
        issues.append(
            f"ring bore {e['bore']:.1f} >= lap G {e['G']:.1f} (lap must sit outside bore)"
        )

    if e["B"] > e["tf"] + 0.01:
        issues.append(f"lap B {e['B']:.2f} > ring tf {e['tf']:.2f}")

    # West / east ring must not share solid span (mirror toward pipe each side).
    if _overlap(e["west_ring"], e["east_ring"]) > 0.5:
        issues.append(
            f"ring solids overlap axially {_overlap(e['west_ring'], e['east_ring']):.1f} mm"
        )

    # Nut must sit outside ring plate toward pipe (no positive overlap into plate span).
    w_plate = e["west_plate"]
    w_nut = e["west_nut"]
    if w_nut[1] > w_plate[0] + 0.05:
        issues.append(
            f"west nut [{w_nut[0]:.1f},{w_nut[1]:.1f}] overlaps plate [{w_plate[0]:.1f},{w_plate[1]:.1f}]"
        )

    e_plate = e["east_plate"]
    e_nut = e["east_nut"]
    if e_nut[0] < e_plate[1] - 0.05:
        issues.append(
            f"east nut [{e_nut[0]:.1f},{e_nut[1]:.1f}] overlaps plate [{e_plate[0]:.1f},{e_plate[1]:.1f}]"
        )

    if abs(e["west_inner"] - e["x_west_bearing"]) > 0.05:
        issues.append(
            f"west bearing drift {e['west_inner']:.2f} vs {e['x_west_bearing']:.2f}"
        )
    if abs(e["east_inner"] - e["x_east_bearing"]) > 0.05:
        issues.append(
            f"east bearing drift {e['east_inner']:.2f} vs {e['x_east_bearing']:.2f}"
        )

    if abs((e["x_east_bearing"] - e["x_west_bearing"]) - e["grip"]) > 0.05:
        issues.append("bearing span != grip")

    # Stub barrel must reach through ring collar (B < ring L).
    if e["F"] - e["B"] < e["ring_L"] - e["tf"] - 1.0:
        issues.append("stub barrel shorter than ring collar depth")

    return issues


def main() -> int:
    all_issues: list[tuple[int, str]] = []
    print("=== LJ CL150 placement audit (joint +X, x=0 west gasket FF) ===\n")
    print(
        f"{'DN':>3} | {'west plate':>14} | {'east plate':>14} | "
        f"{'bearings':>17} | {'nut W':>14} | ok"
    )
    print("-" * 90)

    for dn in LJ_DNS:
        try:
            e = joint_envelope(dn)
            issues = check(dn)
        except (ValueError, KeyError) as ex:
            all_issues.append((dn, str(ex)))
            print(f"{dn:3} | ERROR: {ex}")
            continue

        wp = f"[{e['west_plate'][0]:.1f}, {e['west_plate'][1]:.1f}]"
        ep = f"[{e['east_plate'][0]:.1f}, {e['east_plate'][1]:.1f}]"
        br = f"{e['x_west_bearing']:.1f} .. {e['x_east_bearing']:.1f}"
        wn = f"[{e['west_nut'][0]:.1f}, {e['west_nut'][1]:.1f}]"
        ok = "OK" if not issues else "FAIL"
        print(f"{dn:3} | {wp:>14} | {ep:>14} | {br:>17} | {wn:>14} | {ok}")
        for msg in issues:
            all_issues.append((dn, msg))

    print("\n=== Stack (DN100 example) ===")
    e = joint_envelope(100)
    print(f"  Gasket body          x = [{e['gasket'][0]}, {e['gasket'][1]}]")
    print(f"  West ring (pipe -X)  x = [{e['west_ring'][0]:.1f}, {e['west_ring'][1]:.1f}]  plate [{e['west_plate'][0]:.1f}, 0]")
    print(f"  West stub lap/barrel x = [{e['west_stub_lap'][0]:.1f}, 0] / [{e['west_stub_barrel'][0]:.1f}, {e['west_stub_barrel'][1]:.1f}]")
    print(f"  East ring            x = [{e['east_ring'][0]:.1f}, {e['east_ring'][1]:.1f}]  plate [{e['east_plate'][0]:.1f}, {e['east_plate'][1]:.1f}]")
    print(f"  Stud bearings        x = {e['x_west_bearing']:.1f} .. {e['x_east_bearing']:.1f}  (gap {e['grip']:.2f})")
    print(f"  West nut (pipe side) x = [{e['west_nut'][0]:.1f}, {e['west_nut'][1]:.1f}]")
    print(f"  Ports: stub/ring LAP @ B = {e['B']:.2f} mm")

    print("\n=== Publish checklist ===")
    print("  1. Deploy Catalog + PLANTREGISTERCUSTOMSCRIPTS")
    print("  2. Export Excel -> Publish .pcat")
    print("  3. Re-place joint (delete old components first)")

    if all_issues:
        print(f"\nISSUES ({len(all_issues)}):")
        for dn, msg in all_issues:
            print(f"  DN{dn}: {msg}")
        return 1
    print(f"\nAll {len(LJ_DNS)} sizes: placement OK.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
