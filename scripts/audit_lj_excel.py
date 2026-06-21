"""Audit lap-joint rows in published CATA_NUI.xlsx."""
from __future__ import annotations

import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

XLSX = Path(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.xlsx")
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


def col_letter(n: int) -> str:
    s = ""
    while n:
        n, r = divmod(n - 1, 26)
        s = chr(65 + r) + s
    return s


def load_shared_strings(z: zipfile.ZipFile) -> list[str]:
    data = z.read("xl/sharedStrings.xml")
    root = ET.fromstring(data)
    out: list[str] = []
    for si in root.findall("m:si", NS):
        parts = [t.text or "" for t in si.findall(".//m:t", NS)]
        out.append("".join(parts))
    return out


def cell_value(c, shared: list[str]) -> str:
    t = c.get("t")
    v = c.find("m:v", NS)
    if v is None:
        return ""
    if t == "s":
        return shared[int(v.text)]
    return v.text or ""


def read_sheet(z: zipfile.ZipFile, name: str, shared: list[str]) -> list[dict[str, str]]:
    path = f"xl/worksheets/{name}.xml"
    root = ET.fromstring(z.read(path))
    rows: list[dict[str, str]] = []
    header: dict[str, str] = {}
    for row in root.findall(".//m:sheetData/m:row", NS):
        cells: dict[int, str] = {}
        for c in row.findall("m:c", NS):
            ref = c.get("r", "")
            col = "".join(ch for ch in ref if ch.isalpha())
            col_idx = 0
            for ch in col:
                col_idx = col_idx * 26 + (ord(ch) - 64)
            cells[col_idx] = cell_value(c, shared)
        if not cells:
            continue
        if not header:
            header = {col_letter(i): cells[i] for i in sorted(cells)}
            continue
        rec = {header[col_letter(i)]: cells.get(i, "") for i in sorted(header, key=lambda x: sum((ord(c) - 64) * 26**k for k, c in enumerate(reversed(x))))}
        rows.append(rec)
    return rows


def main() -> None:
    with zipfile.ZipFile(XLSX) as z:
        shared = load_shared_strings(z)
        wb = ET.fromstring(z.read("xl/workbook.xml"))
        rels = ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))
        relmap = {r.get("Id"): r.get("Target") for r in rels}
        sheets = []
        for sh in wb.findall("m:sheets/m:sheet", NS):
            rid = sh.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id")
            target = relmap[rid].replace("worksheets/", "")
            sheets.append((sh.get("name"), target.replace(".xml", "")))

        targets = [s for s in sheets if any(k in s[0] for k in ("STUBEND", "LJ_RING"))]
        print("Sheets:", [s[0] for s in targets])

        port_cols = [
            "DN", "PnPClassName", "PartCategory", "ShortDescription",
            "EndType_S1", "EndType_S2", "PortName_S1", "PortName_S2",
            "NominalDiameter_S1", "NominalDiameter_S2",
            "WallThickness_S1", "WallThickness_S2",
            "EngagementLength_S1", "EngagementLength_S2",
            "Facing_S1", "Facing_S2", "PressureClass_S1", "PressureClass_S2",
            "L", "B", "D1", "D2", "FlangeOffset", "ContentIsoSymbolDefinition",
        ]

        for sheet_name, xml_name in targets:
            rows = read_sheet(z, xml_name, shared)
            print(f"\n=== {sheet_name} ({len(rows)} rows) ===")
            dn50 = [r for r in rows if r.get("DN") in ("50", "50.0", 50)]
            sample = dn50[0] if dn50 else (rows[0] if rows else {})
            for col in port_cols:
                if col in sample and sample[col]:
                    print(f"  {col}: {sample[col]}")
            missing = [c for c in ("EndType_S1", "EndType_S2", "PnPClassName") if not sample.get(c)]
            if missing:
                print("  MISSING:", missing)


if __name__ == "__main__":
    main()
