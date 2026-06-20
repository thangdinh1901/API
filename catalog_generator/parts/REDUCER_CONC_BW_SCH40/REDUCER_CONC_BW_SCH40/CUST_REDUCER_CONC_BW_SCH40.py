"""ASME B16.9 concentric butt weld reducer — pipe OD Sch-40, end-to-end H per B16.9.

Default orientation: run along X; large end at -X, small end at +X.
"""

import pipe_sizes
import primitives as prim


class REDUCERCONCBWSCH40(prim.ShapeObject):
    def __init__(self, s, dn_large, dn_small, *, add_ports=True):
        dn_l = pipe_sizes.resolve_dn(dn_large)
        dn_s = pipe_sizes.resolve_dn(dn_small)
        self.dn = dn_l
        self.dn2 = dn_s
        self.OD_L = pipe_sizes.pipe_od_sch40_mm(dn_l)
        self.OD_S = pipe_sizes.pipe_od_sch40_mm(dn_s)
        self.H = pipe_sizes.bw_reducer_end_to_end_mm(dn_l, dn_s)

        half = self.H / 2.0
        body = prim.Cone(
            s,
            bottom_diameter=self.OD_L,
            top_diameter=self.OD_S,
            height=self.H,
        ).rotateY(90).move(x=-half)

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Butt-weld ports at large (-X) and small (+X) faces."""
        half = self.H / 2.0
        prim.set_port(
            s,
            prim.Point3d(-half, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(half, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
