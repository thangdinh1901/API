"""ASME B16.9 eccentric butt weld reducer — flat bottom (-Z), run along X after rotate.

Same H as concentric reducer; small-end centerline offset for flat-bottom alignment.
"""

import pipe_sizes
import primitives as prim


class REDUCERECCBWSCH40(prim.ShapeObject):
    def __init__(self, s, dn_large, dn_small, *, add_ports=True):
        dn_l = pipe_sizes.resolve_dn(dn_large)
        dn_s = pipe_sizes.resolve_dn(dn_small)
        self.dn = dn_l
        self.dn2 = dn_s
        self.OD_L = pipe_sizes.pipe_od_sch40_mm(dn_l)
        self.OD_S = pipe_sizes.pipe_od_sch40_mm(dn_s)
        self.H = pipe_sizes.bw_reducer_end_to_end_mm(dn_l, dn_s)
        self.ecc = (self.OD_L - self.OD_S) / 2.0

        half = self.H / 2.0
        body = prim.Cone(
            s,
            bottom_diameter=self.OD_L,
            top_diameter=self.OD_S,
            height=self.H,
            eccentricity=self.ecc,
        ).rotateY(90).move(x=-half)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Large port on centerline; small port offset for eccentric flat bottom."""
        half = self.H / 2.0
        z_small = -self.ecc
        prim.set_port(
            s,
            prim.Point3d(-half, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(half, 0.0, z_small),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
