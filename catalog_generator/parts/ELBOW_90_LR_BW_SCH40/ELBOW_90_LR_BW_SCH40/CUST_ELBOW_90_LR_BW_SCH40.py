"""ASME B16.9 90° long-radius butt weld elbow — pipe OD per B36.10 Sch-40."""

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
        self._body = body
        super().__init__(body.obj if hasattr(body, "obj") else body)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Port 1 inline (BV); port 2 outlet to pipe run (PL in catalog metadata)."""
        ix, iy, _ = self._body.arc_inlet_position()
        ox, oy, _ = self._body.arc_outlet_position()
        dx, dy, _ = self._body.arc_outlet_direction()
        prim.set_port(s, prim.Point3d(ix, iy, 0.0), prim.Point3d(1.0, 0.0, 0.0))
        prim.set_port(s, prim.Point3d(ox, oy, 0.0), prim.Point3d(dx, dy, 0.0))
        return self
