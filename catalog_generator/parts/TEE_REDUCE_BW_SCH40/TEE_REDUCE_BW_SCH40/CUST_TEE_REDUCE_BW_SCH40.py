"""ASME B16.9 reducing butt weld tee — run DN large, branch DN small, Sch-40 OD.

Default orientation: run along X; branch along +Y (reduced outlet).
"""

import pipe_sizes
import primitives as prim


class TEEREDUCEBWSCH40(prim.ShapeObject):
    def __init__(self, s, dn_run, dn_branch, *, add_ports=True):
        dn_r = pipe_sizes.resolve_dn(dn_run)
        dn_b = pipe_sizes.resolve_dn(dn_branch)
        self.dn = dn_r
        self.dn2 = dn_b
        self.OD_R = pipe_sizes.pipe_od_sch40_mm(dn_r)
        self.OD_B = pipe_sizes.pipe_od_sch40_mm(dn_b)
        self.C, self.M = pipe_sizes.bw_tee_reducing_center_to_end_mm(dn_r, dn_b)

        run = prim.Cylinder(s, diameter=self.OD_R, height=2.0 * self.C).rotateY(90).move(
            x=-self.C
        )
        branch = prim.Cylinder(s, diameter=self.OD_B, height=self.M).rotateX(-90)
        body = run.combine(branch)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
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
            prim.Point3d(0.0, self.M, 0.0),
            prim.Point3d(0.0, 1.0, 0.0),
        )
        return self
