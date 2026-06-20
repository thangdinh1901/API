"""ASME B16.9 90° long-radius butt weld elbow — pipe OD per B36.10 Sch-40.

Dimensions (Pipedata Pro / B16.9-2018):
    OD  <- pipe outside diameter (mm), Sch-40
    R   <- center-to-face on long radius 90° elbow (mm)

Default orientation: bend in XY plane; port 1 along +X at (R,0,0), port 2 along +Y at (0,R,0).
"""

import pipe_sizes
import primitives as prim


class ELBOW90LRBWSCH40(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.OD = pipe_sizes.pipe_od_sch40_mm(dn)
        self.R = pipe_sizes.bw_elbow_lr90_center_to_face_mm(dn)

        body = prim.Elbow(
            s,
            diameter=self.OD,
            bend_radius=self.R,
            angle=90,
        )
        super().__init__(body.obj if hasattr(body, "obj") else body)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Two butt-weld ports (BV) at center-to-face on each leg."""
        prim.set_port(
            s,
            prim.Point3d(self.R, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(0.0, self.R, 0.0),
            prim.Point3d(0.0, 1.0, 0.0),
        )
        return self
