"""ASME B16.9 45° long-radius butt weld elbow — pipe OD per B36.10 Sch-40.

Plant 3D ARC3D (same as Spec Editor Part Size Properties):
    A  — bend angle (45)
    R  — parametric radius so center-to-face = R * tan(A/2) = B (B16.9 col B)
    L1, L2 — 0 (no extra straight legs in catalog primitive)

Physical LR bend radius (B16.9 col A) is R90; ARC3D expects R = B / tan(A/2), not R90.
Example 2-1/2": B=1.75" -> R=4.2249" in Spec Editor (= 1.75 / tan(22.5°)).

Default orientation: bend in XY; port 1 along +X, port 2 at 45° toward +Y.
"""

import math

import pipe_sizes
import primitives as prim


class ELBOW45LRBWSCH40(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.OD = pipe_sizes.pipe_od_sch40_mm(dn)
        self.B = pipe_sizes.bw_elbow_lr45_center_to_face_mm(dn)
        self.R90 = pipe_sizes.bw_elbow_lr90_center_to_face_mm(dn)
        arc_r = self.B / math.tan(math.radians(22.5))

        body = prim.Elbow(
            s,
            diameter=self.OD,
            bend_radius=arc_r,
            angle=45,
        )
        super().__init__(body.obj if hasattr(body, "obj") else body)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Two butt-weld ports (BV) at B16.9 center-to-face."""
        c = self.B * math.sqrt(2) / 2.0
        prim.set_port(
            s,
            prim.Point3d(self.B, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(c, c, 0.0),
            prim.Point3d(1.0, 1.0, 0.0),
        )
        return self
