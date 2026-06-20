"""ASME B16.9 45° long-radius butt weld elbow — pipe OD per B36.10 Sch-40.

Plant 3D ARC3D:
    A  — bend angle (45)
    R  — B / tan(A/2) so center-to-face on leg 1 = centerX = B
    Port 1 — (B, 0) along +X
    Port 2 — CW arc end (~(-B/√2, B/√2)), direction from ARC3D directionAt(1)
    Catalog: FirstPortEndtypes BV,BV — pipe stock is PL (plain); joints PL↔BV via spec.
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
        arc_r = self.B / math.tan(math.radians(22.5))

        body = prim.Elbow(
            s,
            diameter=self.OD,
            bend_radius=arc_r,
            angle=45,
        )
        self._body = body
        super().__init__(body.obj if hasattr(body, "obj") else body)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Two butt-weld ports (BV) at ARC3D face centers."""
        ix, iy, _ = self._body.arc_inlet_position()
        ox, oy, _ = self._body.arc_outlet_position()
        dx, dy, _ = self._body.arc_outlet_direction()
        prim.set_port(s, prim.Point3d(ix, iy, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(ox, oy, 0.0), prim.Point3d(dx, dy, 0.0))
        return self
