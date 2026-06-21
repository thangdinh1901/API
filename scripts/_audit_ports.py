import sqlite3

def all_ports(p, label, desc, nd):
    print(f"\n=== {label}: {desc} ND={nd} ===")
    c = sqlite3.connect(p)
    for row in c.execute(
        """
        SELECT e.PnPID, e.ShortDescription, pp.Name, pt.EndType, pt.NominalDiameter,
               pt.EngagementLength, pt.WallThickness, pt.Facing, pt.PressureClass
        FROM EngineeringItems e
        JOIN PartPort pp ON pp.Part = e.PnPID
        JOIN Port pt ON pt.PnPID = pp.Port
        WHERE ABS(e.NominalDiameter - ?) < 0.01
          AND e.ShortDescription = ?
        ORDER BY e.PnPID, pp.Name
        """,
        (nd, desc),
    ):
        print(row)
    c.close()

all_ports(r"C:\AutoCAD Plant 3D 2026 Content\CPak ASME\ASME Pipes and Fittings Catalog.pcat", "ASME", "STUB-END FOR LAP FLANGE", 2.0)
all_ports(r"C:\AutoCAD Plant 3D 2026 Content\CPak ASME\ASME Pipes and Fittings Catalog.pcat", "ASME", "FLANGE LJ", 2.0)
all_ports(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat", "CATA", "STUB-END FOR LAP FLANGE", 50.0)
all_ports(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat", "CATA", "FLANGE LJ", 50.0)

# Check if S1 exists anywhere on stubs
c = sqlite3.connect(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat")
print("\n=== CATA stub port names distribution ===")
for row in c.execute(
    """
    SELECT pp.Name, COUNT(*)
    FROM EngineeringItems e
    JOIN PartPort pp ON pp.Part = e.PnPID
    WHERE e.ShortDescription LIKE '%STUB%'
    GROUP BY pp.Name
    """
):
    print(row)
print("LJ ring port names:")
for row in c.execute(
    """
    SELECT pp.Name, pt.EndType, COUNT(*)
    FROM EngineeringItems e
    JOIN PartPort pp ON pp.Part = e.PnPID
    JOIN Port pt ON pt.PnPID = pp.Port
    WHERE e.ShortDescription = 'FLANGE LJ'
    GROUP BY pp.Name, pt.EndType
    """
):
    print(row)
c.close()
