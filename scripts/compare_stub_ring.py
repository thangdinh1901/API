"""Compare stub end vs backing ring dimensions before catalog build."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "catalog_generator"))
sys.path.insert(0, str(ROOT / "catalog_generator" / "parts"))

import pipe_sizes  # noqa: E402
import lj_stud_bolts  # noqa: E402

WN_PATH = ROOT / "catalog_generator/parts/WN_FLRF_CL150/WN_FLRF_CL150/CUST_WN_FLRF_CL150.py"


def wn_g_by_dn() -> dict[int, float]:
    text = WN_PATH.read_text(encoding="utf-8")
    return {int(dn): float(g) for dn, g in re.findall(r'(\d+):\s*\{[^}]*"G":\s*([\d.]+)', text)}


def main() -> None:
    wn_g = wn_g_by_dn()
    dns = [15, 20, 25, 32, 40, 50, 65, 80, 90, 100, 125, 150, 200, 250, 300, 350, 400, 450]
    issues: list[tuple[int, str]] = []

    print(
        f"{'DN':>3} | {'do':>4} | {'pipe':>5} | {'lapG':>5} | {'WN_G':>5} | "
        f"{'ringO':>5} | {'bore':>6} | {'iplexID':>7} | {'clr':>5} | {'O/G':>5} | notes"
    )
    print("-" * 98)

    for dn in dns:
        st = pipe_sizes.stubend_lj_a_dims_mm(dn, "long")
        ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
        iplex = pipe_sizes.iplex_backing_ring_cl150_mm(dn)
        pipe_od = pipe_sizes.pipe_od_sch40_mm(dn)
        wg = wn_g.get(dn, 0.0)
        clr = ring["model_bore"] - st["G"]
        ratio = ring["O"] / st["G"]
        notes: list[str] = []

        if st["G"] >= ring["O"]:
            notes.append("LAP>=RING_OD")
            issues.append((dn, "lap G >= ring OD"))
        if ring["model_bore"] <= st["G"]:
            notes.append("BORE<=LAP")
            issues.append((dn, "bore <= lap G"))
        if abs(st["G"] - wg) > 1.5:
            notes.append(f"G vs WN {wg - st['G']:+.1f}")
            issues.append((dn, f"stub G {st['G']} vs WN G {wg}"))
        if abs(st["OD"] - pipe_od) > 1.0:
            notes.append(f"do vs pipe {st['OD'] - pipe_od:+.1f}")
        if st["T"] > ring["tf"]:
            notes.append("lap thk > ring tf!")
            issues.append((dn, f"lap thk {st['T']} > ring tf {ring['tf']}"))
        if ring["model_bore"] == iplex["id_l2"] and iplex["id_l2"] > st["G"] + 1.5:
            notes.append("iplex L2 drives bore")

        print(
            f"{dn:3} | {st['OD']:4.0f} | {pipe_od:5.1f} | {st['G']:5.0f} | {wg:5.1f} | "
            f"{ring['O']:5.0f} | {ring['model_bore']:6.1f} | {iplex['id_l2']:7.0f} | "
            f"{clr:5.1f} | {ratio:4.2f}x | {', '.join(notes) or 'OK'}"
        )

    print(f"\nTotal issues: {len(issues)}")
    for item in issues:
        print(" -", item)

    dn = 100
    st = pipe_sizes.stubend_lj_a_dims_mm(dn, "long")
    ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
    rf = lj_stud_bolts.lj_stud_length_mm(dn)
    print(f"\n=== DN100 joint check (mm) ===")
    print(f"Stub lap G={st['G']} (gasket seat)  ~= WN RF G={wn_g[dn]}  OK")
    print(f"Backing ring OD={ring['O']} (CL150 flange OD)  >> lap G  ({st['G']/ring['O']*100:.0f}% of ring OD)")
    print(f"Ring bore={ring['model_bore']}  clears lap by {ring['model_bore']-st['G']:.1f} mm")
    print(f"Stub lap thk={st['T']}  <<  ring tf={ring['tf']}  (lap slides inside ring bore)")
    print(f"Ring: FL@0 LAP@stub B={st['T']:.2f} mm, body L={ring['L']} mm (plate+collar)")
    print(
        f"LJ stud OAL={rf['L']} mm  gap={rf['grip']:.2f} mm  "
        f"proj={rf['protrusion_mm']:.3f} mm"
    )


if __name__ == "__main__":
    main()
