import math

import pipe_sizes
import primitives as prim


class LJRINGCL150RF(prim.ShapeObject):
    """Lap-joint backing ring CL150 FF — Iplex plate + Plant catalog collar length L.

    Part-local +X after rotateY(90):
      x=0..tf   FF flange plate (bolt holes)
      x=tf..L   collar over stub pipe barrel (bore = pipe OD, not lap OD)
      FL @0 −X; LAP @stub lap B +X (CollarLapped mates stub shoulder; catalog L = overall depth).
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
        self._collar_h = max(self.L - self.tf, 0.5)
        self.lap_port_x = float(d["stub_lap_t"])

        plate = prim.Cylinder(s, diameter=self.O, height=self.tf)
        collar = prim.Cylinder(s, diameter=self.O, height=self._collar_h).move(
            z=self.tf
        )
        body = plate.combine([collar])
        bore = prim.Cylinder(s, diameter=self.bore_od, height=self.L + 2.0).move(
            z=-1.0
        )

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

        body.subtract([bore] + holes)
        body.rotateY(90)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """FL @ gasket x=0. LAP @ stub lap thickness B — same axial point as stub shoulder."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self.lap_port_x, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
