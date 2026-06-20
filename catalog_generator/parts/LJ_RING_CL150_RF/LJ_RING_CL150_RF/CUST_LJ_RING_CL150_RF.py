import math

import pipe_sizes
import primitives as prim


class LJRINGCL150RF(prim.ShapeObject):
    """Lap-joint backing ring CL150 FF — flat plate (Iplex / Plant FLANGE LJ)."""

    def __init__(self, s, size, *, add_ports=True):
        key = pipe_sizes.resolve_dn(size)
        d = pipe_sizes.lj_ring_cl150_dims_mm(key)

        self.dn = key
        self.nps = pipe_sizes.dn_to_nps(key)
        self.O = d["O"]
        self.tf = d["tf"]
        self.bore_od = d["model_bore"]
        self.bcd = d["bcd"]
        self.hd = d["hd"]
        self.n = int(d["n"])

        axial = d["port_len"]
        self._lap_t = d["stub_lap_t"]
        disc = prim.Cylinder(s, diameter=self.O, height=self.tf)
        bore = prim.Cylinder(s, diameter=self.bore_od, height=axial + 2).move(z=-1)

        holes = []
        offset_deg = 360.0 / self.n / 2.0
        for i in range(self.n):
            ang = math.radians(i * 360.0 / self.n + offset_deg)
            hx = (self.bcd / 2.0) * math.cos(ang)
            hy = (self.bcd / 2.0) * math.sin(ang)
            hole = prim.Cylinder(s, diameter=self.hd, height=self.tf + 2).move(
                x=hx, y=hy, z=-1
            )
            holes.append(hole)

        body = disc.subtract([bore] + holes)
        body.rotateY(90)

        self._total_len = axial

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """FL west (bolt face toward joint). LAP east — mates stub LAP at x=0."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self._lap_t, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
