"""Compare NPMC vs CS150 spec completeness."""
import sqlite3
from pathlib import Path

def summary(path: str, label: str) -> None:
    c = sqlite3.connect(path)
    print(f"\n=== {label} ===")
    for cls in ("Flange", "BlindFlange", "Gasket", "BoltSet", "StubEnd", "Collar", "Pipe"):
        try:
            n = c.execute(f"SELECT COUNT(*) FROM [{cls}]").fetchone()[0]
            print(f"  {cls}: {n}")
        except Exception:
            pass
    c.close()

summary(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\NPMC.pspc", "NPMC")
summary(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CS150.pspc", "CS150")
