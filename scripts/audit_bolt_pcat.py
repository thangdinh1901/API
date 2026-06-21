import sqlite3

c = sqlite3.connect(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat")
cols = [r[1] for r in c.execute("PRAGMA table_info(EngineeringItems)").fetchall()]
print("cols", [x for x in cols if "Bolt" in x or "Length" in x or "Stud" in x])
rows = c.execute(
    """
    SELECT NominalDiameter, ShortDescription, BoltSize, NumberInSet, Length
    FROM EngineeringItems
    WHERE ShortDescription LIKE '%Bolt%'
      AND NominalDiameter IN (50, 100)
    ORDER BY NominalDiameter, ShortDescription
    """
).fetchall()
print("bolt rows", rows)
c.close()
