"""ASME B16.9 90° short-radius butt weld elbow — pipe OD per B36.10 Sch-40.

SR 90°: center-to-face A = 1D (1 × NPS). For ARC3D at 90°, bend_radius = A.
Available NPS 1" (DN25) and larger per B16.9; no SR for DN15/DN20.
"""

import pipe_sizes
import primitives as prim


class ELBOW90SRBWSCH40(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.OD = pipe_sizes.pipe_od_sch40_mm(dn)
        self.A = pipe_sizes.bw_elbow_sr90_center_to_face_mm(dn)

        body = prim.Elbow(
            s,
            diameter=self.OD,
            bend_radius=self.A,
            angle=90,
        )
        super().__init__(body.obj if hasattr(body, "obj") else body)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Two butt-weld ports (BV) at B16.9 center-to-face on each leg."""
        prim.set_port(
            s,
            prim.Point3d(self.A, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(0.0, self.A, 0.0),
            prim.Point3d(0.0, 1.0, 0.0),
        )
        return self
