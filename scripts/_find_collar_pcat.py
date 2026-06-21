import os
import sqlite3

root = r"C:\AutoCAD Plant 3D 2026 Content"
found = []
for dp, _, fs in os.walk(root):
    for f in fs:
        if not f.endswith(".pcat"):
            continue
        p = os.path.join(dp, f)
        try:
            c = sqlite3.connect(p)
            tabs = {t[0] for t in c.execute("SELECT name FROM sqlite_master WHERE type='table'")}
            if "Collar" in tabs:
                n = c.execute("SELECT COUNT(*) FROM Collar").fetchone()[0]
                if n:
                    found.append((p, n))
            c.close()
        except Exception:
            pass

print("pcats with Collar data:", len(found))
for p, n in found[:15]:
    print(n, p)
