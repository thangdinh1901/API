"""ASME B16.9 equal butt weld tee — pipe OD Sch-40, center-to-end per B16.9.

Default orientation: run along X; branch outlet along +Y.
"""

import pipe_sizes
import primitives as prim


class TEEEQBWSCH40(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.OD = pipe_sizes.pipe_od_sch40_mm(dn)
        self.C = pipe_sizes.bw_tee_equal_center_to_end_mm(dn)

        run = prim.Cylinder(s, diameter=self.OD, height=2.0 * self.C).rotateY(90).move(
            x=-self.C
        )
        branch = prim.Cylinder(s, diameter=self.OD, height=self.C).rotateX(-90)
        body = run.combine(branch)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Three butt-weld ports: run -X / +X, branch +Y."""
        prim.set_port(
            s,
            prim.Point3d(-self.C, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self.C, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(0.0, self.C, 0.0),
            prim.Point3d(0.0, 1.0, 0.0),
        )
        return self
