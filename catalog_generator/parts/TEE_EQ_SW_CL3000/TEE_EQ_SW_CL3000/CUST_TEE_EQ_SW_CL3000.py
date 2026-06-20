"""ASME B16.11 Class 3000 equal socket weld tee — ports at outer socket face A + J."""

import pipe_sizes
import primitives as prim
import sw_fitting_geom as sw


class TEEEQSWCL3000(prim.ShapeObject):
    def __init__(self, s, size, *, add_ports=True):
        dn = pipe_sizes.resolve_sw_dn(size)
        self.dn = dn
        self.nps = pipe_sizes.sw_dn_to_nps(dn)
        self.pipe_OD = pipe_sizes.sw_pipe_od_mm(dn)
        self.A = pipe_sizes.sw_cl3000_center_to_socket_mm(dn)
        self.L = pipe_sizes.sw_cl3000_center_to_outer_socket_mm(dn)

        # Run/branch end at inner socket shoulder (A); forging spans A → L per port.
        run = prim.Cylinder(s, diameter=self.pipe_OD, height=2.0 * self.A).rotateY(90).move(
            x=-self.A
        )
        branch = prim.Cylinder(s, diameter=self.pipe_OD, height=self.A).rotateX(-90)
        body = run.combine(branch)

        ports = [
            (dn, prim.Point3d(-self.L, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0)),
            (dn, prim.Point3d(self.L, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0)),
            (dn, prim.Point3d(0.0, self.L, 0.0), prim.Point3d(0.0, 1.0, 0.0)),
        ]
        sw.apply_socket_rings(body, s, ports)
        super().__init__(body.obj)

        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(
            s,
            prim.Point3d(-self.L, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
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
