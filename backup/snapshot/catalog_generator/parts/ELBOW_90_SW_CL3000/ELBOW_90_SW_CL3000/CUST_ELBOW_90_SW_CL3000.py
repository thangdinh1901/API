"""ASME B16.11 Class 3000 socket weld 90° elbow.

A = center to inner socket shoulder; port at outer face L = A + J (e.g. DN25: 35 mm).
"""

import pipe_sizes
import primitives as prim
import sw_fitting_geom as sw


class ELBOW90SWCL3000(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_sw_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.sw_dn_to_nps(dn)
        self.pipe_OD = pipe_sizes.sw_pipe_od_mm(dn)
        self.A = pipe_sizes.sw_cl3000_center_to_socket_mm(dn)
        self.J = pipe_sizes.sw_cl3000_socket_depth_mm(dn)
        self.L = pipe_sizes.sw_cl3000_center_to_outer_socket_mm(dn)

        # Bend ends at inner socket shoulder (A); forging collar alone spans A → L.
        body = prim.Elbow(
            s,
            diameter=self.pipe_OD,
            bend_radius=self.A,
            angle=90,
        )

        ports = [
            (dn, prim.Point3d(self.L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0)),
            (dn, prim.Point3d(0.0, self.L, 0.0), prim.Point3d(0.0, 1.0, 0.0)),
        ]
        sw.apply_elbow_socket_collars(body, s, ports)
        super().__init__(body.obj if hasattr(body, "obj") else body)

        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(
            s,
            prim.Point3d(self.L, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(0.0, self.L, 0.0),
            prim.Point3d(0.0, 1.0, 0.0),
        )
        return self
