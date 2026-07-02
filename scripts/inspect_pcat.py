import sqlite3, sys

path = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\dinht\Downloads\TEST.pcat"
con = sqlite3.connect(path)
con.row_factory = sqlite3.Row
cur = con.cursor()

print("== Elbow table (routing geometry) ==")
cur.execute("""SELECT e.PnPID, ei.NominalDiameter, e.PathAngle, e.CurveRadius, e.SegmentCount,
                      ei.ContentGeometryTemplate, ei.EndType, ei.PartCategory
               FROM Elbow e JOIN EngineeringItems ei ON ei.PnPID = e.PnPID
               ORDER BY ei.NominalDiameter""")
for r in cur.fetchall():
    print(dict(r))

print()
print("== PnPBase class breakdown ==")
cur.execute("SELECT PnPClassName, COUNT(*) FROM PnPBase GROUP BY PnPClassName")
for r in cur.fetchall():
    print(dict(r))

print()
print("== Port table sample ==")
cur.execute("SELECT * FROM Port LIMIT 3")
for r in cur.fetchall():
    print({k: r[k] for k in r.keys()})

con.close()
