import sqlite3

p = r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat"
c = sqlite3.connect(p)

print("=== DN50 stub/ring in CATA ===")
for row in c.execute(
    """
    SELECT e.PnPID, e.ShortDescription, e.PartCategory, e.NominalDiameter,
           e.ContentIsoSymbolDefinition, e.PartFamilyLongDesc
    FROM EngineeringItems e
    WHERE e.NominalDiameter = 50
      AND (e.ShortDescription LIKE '%STUB%' OR e.ShortDescription = 'FLANGE LJ')
    """
):
    print(row)

# Check if PnPClassName exists
cols = [x[1] for x in c.execute("PRAGMA table_info(EngineeringItems)")]
print("Has PnPClassName", "PnPClassName" in cols)

# PartPort for DN50 stub
for row in c.execute(
    """
    SELECT e.ShortDescription, p.PortName, p.EndType, p.NominalDiameter, p.EngagementLength,
           p.WallThickness, p.PressureClass, p.Facing
    FROM EngineeringItems e
    JOIN PartPort p ON p.PnPID = e.PnPID
    WHERE e.NominalDiameter = 50 AND e.ShortDescription LIKE '%STUB%'
  """
):
    print("port", row)

# StubEnd table
for row in c.execute(
    """
    SELECT e.ShortDescription, s.FlangeOffset
    FROM StubEnd s JOIN EngineeringItems e ON e.PnPID=s.PnPID
    WHERE e.NominalDiameter=50
    """
):
    print("StubEnd row", row)

# Flange table for LJ ring
for row in c.execute(
    """
    SELECT e.ShortDescription, f.*
    FROM Flange f JOIN EngineeringItems e ON e.PnPID=f.PnPID
    WHERE e.NominalDiameter=50 AND e.ShortDescription='FLANGE LJ'
    LIMIT 1
    """
):
    print("Flange LJ", row)

c.close()
