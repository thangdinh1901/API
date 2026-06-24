import math

import pipe_sizes
import primitives as prim


class LJRINGCL150RF(prim.ShapeObject):
    """Lap-joint backing ring CL150 FF — flat plate (ASME B16.5 Table 7 O/W, Table 8 tf).

    Part-local +X after rotateY(90):
      x=0..tf   FF flat plate (OD O, bore model_bore, bolt holes on PCD W)
      FL @0 −X; LAP @tf +X (stub side).
    """

    def __init__(self, s, size, *, add_ports=True):
        key = pipe_sizes.resolve_dn(size)
        d = pipe_sizes.lj_ring_cl150_dims_mm(key)

        self.dn = key
        self.nps = pipe_sizes.dn_to_nps(key)
        self.O = d["O"]
        self.tf = d["tf"]
        self.L = d["L"]
        self.bore_od = d["model_bore"]
        self.bcd = d["bcd"]
        self.hd = d["hd"]
        self.n = int(d["n"])

        plate = prim.Cylinder(s, diameter=self.O, height=self.tf)
        bore = prim.Cylinder(s, diameter=self.bore_od, height=self.tf + 2.0).move(z=-1.0)

        holes = []
        offset_deg = 360.0 / self.n / 2.0
        for i in range(self.n):
            ang = math.radians(i * 360.0 / self.n + offset_deg)
            hx = (self.bcd / 2.0) * math.cos(ang)
            hy = (self.bcd / 2.0) * math.sin(ang)
            hole = prim.Cylinder(s, diameter=self.hd, height=self.tf + 2.0).move(
                x=hx, y=hy, z=-1.0
            )
            holes.append(hole)

        body = plate.subtract([bore] + holes)
        body.rotateY(90)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """FL @ gasket face x=0; LAP @ stub-side face x=tf."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self.tf, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
