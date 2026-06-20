"""Merge catalog_entry.py + geometry script (same as CatalogDeployService.MergeCatalogPartPy)."""
from __future__ import annotations

import re
import sys
from pathlib import Path

SUBFOLDER_IMPORT = re.compile(r"^from [A-Z0-9_]+\.CUST_[A-Z0-9_]+ import ")


def merge(entry_path: Path, geometry_path: Path) -> str:
    entry_lines = entry_path.read_text(encoding="utf-8").splitlines()
    geom = geometry_path.read_text(encoding="utf-8").strip()
    geom = re.sub(
        r"^from ([A-Z0-9_]+)\.CUST_\1 import (.+)$",
        r"from CUST_\1 import \2",
        geom,
        flags=re.MULTILINE,
    )
    geom_lines = [
        line.rstrip("\r")
        for line in geom.splitlines()
        if "varmain.custom" not in line and not line.startswith("# Port Manager:")
    ]
    while geom_lines and not geom_lines[0].strip():
        geom_lines.pop(0)
    geom = "\n".join(geom_lines).strip()

    varmain = [line for line in entry_lines if "varmain.custom" in line]
    body = [
        line
        for line in entry_lines
        if "varmain.custom" not in line and not SUBFOLDER_IMPORT.match(line)
    ]
    activate_idx = next((i for i, line in enumerate(body) if "@activate" in line), -1)
    if activate_idx >= 0:
        body = body[activate_idx:]

    chunks = []
    if varmain:
        chunks.append("\n".join(varmain))
    if geom:
        chunks.append(geom)
    body_text = "\n".join(body).strip()
    if body_text:
        chunks.append(body_text)
    return "\n\n".join(chunks).strip() + "\n"


def main() -> int:
    if len(sys.argv) != 4:
        print("Usage: merge_catalog_part.py <entry.py> <geometry.py> <output.py>")
        return 1
    out = merge(Path(sys.argv[1]), Path(sys.argv[2]))
    Path(sys.argv[3]).write_text(out, encoding="utf-8", newline="\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
