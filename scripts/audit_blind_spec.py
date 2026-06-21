import sqlite3

PSPEC = r"D:\04. Projects\06. NUI\NUI\Spec Sheets\NPMC.pspc"
PCAT = r"D:\04. Projects\06. NUI\NUI\Spec Sheets\CATA_NUI.pcat"
ASME = r"C:\AutoCAD Plant 3D 2026 Content\CPak ASME\ASME Pipes and Fittings Catalog.pcat"


def port_cols(conn):
    cols = [r[1] for r in conn.execute("PRAGMA table_info(EngineeringItems)").fetchall()]
    return [c for c in cols if "Port" in c or "EndType" in c or "Facing" in c or "S1" in c or "ALL" in c]


def show(label, path):
    c = sqlite3.connect(path)
    print(f"\n=== {label} port-ish columns ===")
    cols = port_cols(c)
    print(cols[:25])
    row = c.execute(
        """
        SELECT * FROM EngineeringItems
        WHERE ShortDescription LIKE 'FLANGE BLIND%'
          AND NominalDiameter = ?
        LIMIT 1
        """,
        (100 if "NUI" in label or "spec" in label else 4,),
    ).fetchone()
    if not row:
        print("no row")
        c.close()
        return
    names = [d[0] for d in c.execute(
        "SELECT * FROM EngineeringItems LIMIT 0"
    ).description]
    data = dict(zip(names, row))
    for k in cols:
        if data.get(k) not in (None, "", 0):
            print(f"  {k} = {data[k]}")
    c.close()


show("NUI pcat", PCAT)
show("NPMC spec", PSPEC)
show("ASME pcat", ASME)
