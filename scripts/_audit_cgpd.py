import sqlite3

def audit(path, label):
    c = sqlite3.connect(path)
    print(f"\n=== {label} ===")
    for desc in ("FLANGE WN", "FLANGE SO", "FLANGE LJ", "FLANGE BLIND", "GASKET RF", "GASKET FF"):
        like = "FLANGE BLIND%" if "BLIND" in desc else desc + "%" if "GASKET" in desc else desc
        for nd in (50, 100):
            row = c.execute(
                """
                SELECT e.ContentGeometryParamDefinition, e.ContentGeometryTemplate, e.NominalDiameter
                FROM EngineeringItems e
                WHERE e.ShortDescription LIKE ? AND ABS(e.NominalDiameter - ?) < 0.01
                LIMIT 1
                """,
                (like, nd),
            ).fetchone()
            if row:
                print(f"  {desc} DN{nd}: param={row[0]!r}")
    for nd in (50, 100):
        row = c.execute(
            "SELECT ContentGeometryParamDefinition, EndType, Schedule FROM EngineeringItems WHERE ShortDescription LIKE 'PIPE%' AND NominalDiameter=? LIMIT 1",
            (nd,),
        ).fetchone()
        if row:
            print(f"  PIPE DN{nd}: param={row[0]!r} end={row[1]} sch={row[2]}")
    for row in c.execute(
        "SELECT ShortDescription, NominalDiameter, Facing, PressureClass, EndType FROM EngineeringItems WHERE PnPID IN (SELECT PnPID FROM BoltSet) AND NominalDiameter IN (50,100)"
    ):
        print(f"  bolt {row}")
    c.close()

audit(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\NPMC.pspc", "NPMC SPEC")
audit(r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat", "CATA PCAT")

# Compare ASME WN DN100 equivalent
c = sqlite3.connect(r"C:\AutoCAD Plant 3D 2026 Content\CPak ASME\ASME Pipes and Fittings Catalog.pcat")
row = c.execute(
    "SELECT ContentGeometryParamDefinition FROM EngineeringItems WHERE ShortDescription='FLANGE WN' AND NominalDiameter=4 LIMIT 1"
).fetchone()
print("\nASME WN 4in param:", row)
c.close()
