"""ASME B16.9 Type A long-pattern lap-joint stub end — Sch-40 pipe OD at weld."""

import pipe_sizes
import primitives as prim


class STUBENDLJABWSCH40(prim.ShapeObject):
    """Lap-joint stub end Type A (long pattern).

    West (-X after rotateY): LAP lap face. East (+X): BV weld end.
    Dimensions: G lap OD, T lap thickness, F overall length, pipe OD/ID Sch-40.
    """

    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        dims = pipe_sizes.stubend_lj_a_long_dims_mm(dn)
        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.G = dims["G"]
        self.T = dims["T"]
        self.F = dims["F"]
        self.R = dims["R"]
        self.OD = pipe_sizes.pipe_od_sch40_mm(dn)
        self.ID = pipe_sizes.pipe_id_sch40_mm(dn)

        lap = prim.Cylinder(s, diameter=self.G, height=self.T)
        tail = max(self.F - self.T, self.OD)
        hub_len = min(max(tail * 0.45, self.OD * 0.5), tail - 5.0)
        barrel_len = max(tail - hub_len, 5.0)
        taper = prim.Cone(
            s,
            bottom_diameter=self.G,
            top_diameter=self.OD,
            height=hub_len,
        ).move(z=self.T)
        barrel = prim.Cylinder(s, diameter=self.OD, height=barrel_len).move(
            z=self.T + hub_len
        )
        body = lap.combine([taper, barrel])
        bore = prim.Cylinder(s, diameter=self.ID, height=self.F + 2).move(z=-1)
        body.subtract([bore])
        body.rotateY(90)

        self._length = self.F
        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Port 1 (West): LAP lap face. Port 2 (East): BV weld end."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self._length, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
