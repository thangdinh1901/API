"""Verify lap-joint deploy + export rules before final Plant test."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CS = Path(r"C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts")

REQUIRED = [
    "stubend_geom.py",
    "pipe_sizes.py",
    "lj_stud_bolts.py",
    "CUST_STUBEND_LJ_A_BW_SCH40.py",
    "CUST_STUBEND_LJ_A_SH_BW_SCH40.py",
    "CUST_LJ_RING_CL150_RF.py",
    "CUST_GSK_FF_CL150.py",
]

EXPECTED_PLACEMENT_REV = "2026-06-11-tf-H"

MERGED_CUST_MARKERS: dict[str, tuple[str, ...]] = {
    "CUST_GSK_FF_CL150.py": (
        "lj_stud_start_x_west",
        "LJ_STUD_PLACEMENT_REV",
        EXPECTED_PLACEMENT_REV,
    ),
    "CUST_LJ_RING_CL150_RF.py": ("lap_port_x",),
    "CUST_STUBEND_LJ_A_BW_SCH40.py": ("STUBENDLJABWSCH40",),
    "CUST_STUBEND_LJ_A_SH_BW_SCH40.py": ("STUBENDLJASHBWSCH40",),
}


def md5(p: Path) -> str:
    return hashlib.md5(p.read_bytes()).hexdigest()


def resolve_repo_src(name: str) -> Path:
    if name.startswith("CUST_"):
        part = name.replace("CUST_", "").replace(".py", "")
        nested = ROOT / "catalog_generator" / "parts" / part / part / name
        if nested.exists():
            return nested
        flat = list((ROOT / "catalog_generator" / "parts" / part).rglob(name))
        if flat:
            return flat[0]
    return ROOT / "catalog_generator" / name


def check_cust_deploy(name: str, src: Path, dst: Path, issues: list[str]) -> None:
    if name not in MERGED_CUST_MARKERS:
        if src.exists() and md5(src) != md5(dst):
            issues.append(f"STALE deploy: {name} (repo != CustomScripts)")
        return

    dst_text = dst.read_text(encoding="utf-8")
    src_text = src.read_text(encoding="utf-8") if src.exists() else ""
    for key in MERGED_CUST_MARKERS[name]:
        if key in src_text and key not in dst_text:
            issues.append(f"STALE deploy marker missing in {name}: {key}")


def check_manifest(issues: list[str]) -> None:
    manifest_path = CS / "deploy_manifest.json"
    if not manifest_path.exists():
        issues.append("MISSING deploy_manifest.json — run Deploy Catalog")
        return

    try:
        data = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError as ex:
        issues.append(f"deploy_manifest.json invalid: {ex}")
        return

    print("\n=== deploy_manifest.json ===")
    print(f"  deployedAtUtc: {data.get('deployedAtUtc', data.get('DeployedAtUtc', '?'))}")
    print(f"  pycacheCleared: {data.get('pycacheFoldersCleared', data.get('PycacheFoldersCleared', '?'))}")
    print(f"  registerQueued: {data.get('registerQueued', data.get('RegisterQueued', '?'))}")

    hashes = data.get("keyFileHashes") or data.get("KeyFileHashes") or {}
    for name in ("lj_stud_bolts.py", "CUST_GSK_FF_CL150.py"):
        expected = md5(CS / name) if (CS / name).exists() else None
        manifest_hash = hashes.get(name)
        if expected and manifest_hash and expected != manifest_hash.lower():
            issues.append(f"STALE manifest hash: {name}")


def check_pycache() -> None:
    pycache_dirs = list(CS.rglob("__pycache__"))
    pyc_count = sum(1 for _ in CS.rglob("*.pyc"))
    print("\n=== __pycache__ (after PLANTREGISTERCUSTOMSCRIPTS should exist) ===")
    print(f"  folders: {len(pycache_dirs)}, .pyc files: {pyc_count}")
    if pyc_count == 0:
        print("  WARN: no .pyc yet — run PLANTREGISTERCUSTOMSCRIPTS in Plant 3D")


def main() -> int:
    issues: list[str] = []
    print("=== Deploy sync (repo vs CustomScripts) ===")
    for name in REQUIRED:
        src = resolve_repo_src(name)
        dst = CS / name
        if not dst.exists():
            issues.append(f"MISSING deploy: {name}")
            continue
        check_cust_deploy(name, src, dst, issues)

    lj = CS / "lj_stud_bolts.py"
    if lj.exists():
        lj_text = lj.read_text(encoding="utf-8")
        if EXPECTED_PLACEMENT_REV not in lj_text:
            issues.append(f"lj_stud_bolts: missing placement rev {EXPECTED_PLACEMENT_REV}")
        if "FaceType.RTJ" in lj_text:
            issues.append("lj_stud_bolts: still using B16.5 ring-joint OAL")

    check_manifest(issues)
    check_pycache()

    print("\n=== Export rules (need Publish .pcat after Export) ===")
    print("  Stud: gap=T+2*tf+2*H; OAL=ceil(gap+2*H+6*pitch)")
    print("  Nut bearing west @ x=-tf-H (stud visual in GSK_FF only)")

    print("\n=== Plant test workflow ===")
    print("  1. Deploy Catalog (Composer) OR scripts/Deploy-Catalog.ps1")
    print("  2. PLANTREGISTERCUSTOMSCRIPTS (rebuilds __pycache__)")
    print("  3. DELETE old lap joint -> place new: Stub->Ring->GSK_FF->Ring->Stub")
    print("  4. Isolate GSK_FF in Model Browser to verify hex nuts on gasket solid")

    if issues:
        print(f"\nISSUES ({len(issues)}):")
        for i in issues:
            print(f"  ! {i}")
        return 1
    print("\nDeploy files OK.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
