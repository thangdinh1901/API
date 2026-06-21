import sqlite3

p = r"D:\04. Projects\06. NUI\NUI\Spec Sheets\NPMC.pspc"
c = sqlite3.connect(p)
pid = 37261  # STUB-END FOR LAP FLANGE DN50

tables = [t[0] for t in c.execute("SELECT name FROM sqlite_master WHERE type='table'")]
for t in tables:
    cols = [x[1] for x in c.execute(f"PRAGMA table_info([{t}])")]
    if "PnPID" in cols:
        n = c.execute(f"SELECT COUNT(*) FROM [{t}] WHERE PnPID=?", (pid,)).fetchone()[0]
        if n:
            print(f"{t}: {n}")
    elif "Part" in cols:
        n = c.execute(f"SELECT COUNT(*) FROM [{t}] WHERE Part=?", (pid,)).fetchone()[0]
        if n:
            print(f"{t}.Part: {n}")

c.close()
