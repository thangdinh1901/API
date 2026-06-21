"""ASME B16.9 Type A lap-joint stub end — two cylinders per STUBEND TABLE.

Lap disc G×B at x=0..B; barrel toward pipe.
S1 LAP @ shoulder x=B −X; S2 BV @F +X.
Plant CPMUW: catalog FlangeOffset=B with LAP at shoulder — lap face ends up at gasket.
"""

import pipe_sizes
import primitives as prim


class StubEndLjA(prim.ShapeObject):
    """Lap at x=0 (joint), BV weld at x=F (pipe). Same port frame as WN_FLRF."""

    def __init__(self, s, size, pattern: str = "long", *, add_ports: bool = True):
        dn = pipe_sizes.resolve_dn(size)
        dims = pipe_sizes.stubend_lj_a_dims_mm(dn, pattern)
        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.G = dims["G"]
        self.T = dims["T"]
        self.F = dims["F"]
        self.OD = dims["OD"]
        self.ID = dims["ID"]
        self._length = self.F

        barrel_len = max(self.F - self.T, 1.0)
        lap = prim.Cylinder(s, diameter=self.G, height=self.T)
        barrel = prim.Cylinder(s, diameter=self.OD, height=barrel_len).move(z=self.T)
        body = lap.combine([barrel])
        bore = prim.Cylinder(s, diameter=self.ID, height=self.F + 2).move(z=-1)
        body.subtract([bore])
        body.rotateY(90)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        # S1 LAP @ lap shoulder (native CPMUW + FlangeOffset=B → lap face at gasket).
        prim.set_port(
            s,
            prim.Point3d(self.T, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        # S2 BV — pipe weld at x=F.
        prim.set_port(
            s,
            prim.Point3d(self._length, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
