import sqlite3

p = r"C:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomParts Metric Catalog.pcat"
c = sqlite3.connect(p)

print("=== Collar DN50-ish ===")
for row in c.execute(
    """
    SELECT e.ShortDescription, e.PartCategory, e.NominalDiameter,
           e.ContentIsoSymbolDefinition, col.*
    FROM Collar col
    JOIN EngineeringItems e ON e.PnPID = col.PnPID
    WHERE e.NominalDiameter BETWEEN 49 AND 51
    """
):
    print(row)

print("\n=== All Collar short descriptions ===")
for row in c.execute(
    """
    SELECT DISTINCT e.ShortDescription, e.ContentIsoSymbolDefinition
    FROM Collar col JOIN EngineeringItems e ON e.PnPID = col.PnPID
  """
):
    print(row)

print("\n=== Collar ports DN50 ===")
for row in c.execute(
    """
    SELECT e.ShortDescription, pp.Name, pt.EndType, pt.NominalDiameter, pt.WallThickness
    FROM Collar col
    JOIN EngineeringItems e ON e.PnPID = col.PnPID
    JOIN PartPort pp ON pp.Part = e.PnPID
    JOIN Port pt ON pt.PnPID = pp.Port
    WHERE e.NominalDiameter BETWEEN 49 AND 51
    """
):
    print(row)

print("\n=== Collar table schema ===")
for row in c.execute("PRAGMA table_info(Collar)"):
    print(row)

c.close()
