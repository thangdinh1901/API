"""ASME B16.11 Class 3000 socket weld 45° elbow — port at outer face B + J."""

import math

import pipe_sizes
import primitives as prim
import sw_fitting_geom as sw


class ELBOW45SWCL3000(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_sw_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.sw_dn_to_nps(dn)
        self.pipe_OD = pipe_sizes.sw_pipe_od_mm(dn)
        self.B = pipe_sizes.sw_cl3000_elbow_45_center_to_socket_mm(dn)
        self.J = pipe_sizes.sw_cl3000_socket_depth_mm(dn)
        self.L = pipe_sizes.sw_cl3000_elbow_45_center_to_outer_socket_mm(dn)
        arc_r = self.B / math.tan(math.radians(22.5))

        # Bend ends at inner socket shoulder (B); forging collar alone spans B → L.
        body = prim.Elbow(
            s,
            diameter=self.pipe_OD,
            bend_radius=arc_r,
            angle=45,
        )

        c_out = self.L * math.sqrt(2) / 2.0
        inv_sqrt2 = 1.0 / math.sqrt(2.0)
        ports = [
            (dn, prim.Point3d(self.L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0)),
            (
                dn,
                prim.Point3d(c_out, c_out, 0.0),
                prim.Point3d(inv_sqrt2, inv_sqrt2, 0.0),
            ),
        ]
        sw.apply_elbow_socket_collars(body, s, ports)
        super().__init__(body.obj if hasattr(body, "obj") else body)

        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        c_out = self.L * math.sqrt(2) / 2.0
        prim.set_port(
            s,
            prim.Point3d(self.L, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(c_out, c_out, 0.0),
            prim.Point3d(1.0, 1.0, 0.0),
        )
        return self
