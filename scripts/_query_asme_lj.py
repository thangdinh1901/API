import sqlite3

for label, p in [
    ("ASME", r"C:\AutoCAD Plant 3D 2026 Content\CPak ASME\ASME Pipes and Fittings Catalog.pcat"),
    ("CATA", r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat"),
]:
    print(f"\n======== {label} ========")
    c = sqlite3.connect(p)
    for row in c.execute(
        """
        SELECT ShortDescription, PartCategory, NominalDiameter, ContentIsoSymbolDefinition
        FROM EngineeringItems
        WHERE ShortDescription LIKE '%STUB%' OR ShortDescription = 'FLANGE LJ'
        ORDER BY NominalDiameter
        LIMIT 25
        """
    ):
        print(row)

    tables = {t[0] for t in c.execute("SELECT name FROM sqlite_master WHERE type='table'")}
    if "StubEnd" in tables:
        n = c.execute("SELECT COUNT(*) FROM StubEnd").fetchone()[0]
        print(f"StubEnd rows: {n}")
        for row in c.execute(
            """
            SELECT e.ShortDescription, e.PartCategory, e.NominalDiameter, s.FlangeOffset
            FROM StubEnd s
            JOIN EngineeringItems e ON e.PnPID = s.PnPID
            WHERE e.NominalDiameter BETWEEN 49 AND 51
            """
        ):
            print("  StubEnd DN~50:", row)

    if "Collar" in tables:
        print("Has Collar table")
    else:
        print("No Collar table")

    c.close()
