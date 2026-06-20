"""ASME B16.11 Class 3000 reducing SW tee — C/M inner; ports at C+J / M+J."""

import pipe_sizes
import primitives as prim
import sw_fitting_geom as sw


class TEEREDUCESWCL3000(prim.ShapeObject):
    def __init__(self, s, dn_run, dn_branch, *, add_ports=True):
        dn_r = pipe_sizes.resolve_sw_dn(dn_run)
        dn_b = pipe_sizes.resolve_sw_dn(dn_branch)
        self.dn = dn_r
        self.dn2 = dn_b
        self.pipe_OD_R = pipe_sizes.sw_pipe_od_mm(dn_r)
        self.pipe_OD_B = pipe_sizes.sw_pipe_od_mm(dn_b)
        self.C, self.M = pipe_sizes.sw_tee_reducing_center_to_socket_mm(dn_r, dn_b)
        self.J_R = pipe_sizes.sw_cl3000_socket_depth_mm(dn_r)
        self.J_B = pipe_sizes.sw_cl3000_socket_depth_mm(dn_b)
        self.L_run = self.C + self.J_R
        self.L_branch = self.M + self.J_B

        # Run/branch end at inner socket shoulder (C / M); forging spans to outer face.
        run = prim.Cylinder(s, diameter=self.pipe_OD_R, height=2.0 * self.C).rotateY(
            90
        ).move(x=-self.C)
        branch = prim.Cylinder(s, diameter=self.pipe_OD_B, height=self.M).rotateX(-90)
        body = run.combine(branch)

        ports = [
            (dn_r, prim.Point3d(-self.L_run, 0.0, 0.0), prim.Point3d(-1.0, 0.0, 0.0)),
            (dn_r, prim.Point3d(self.L_run, 0.0, 0.0), prim.Point3d(1.0, 0.0, 0.0)),
            (dn_b, prim.Point3d(0.0, self.L_branch, 0.0), prim.Point3d(0.0, 1.0, 0.0)),
        ]
        sw.apply_socket_rings(body, s, ports)
        super().__init__(body.obj)

        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        prim.set_port(
            s,
            prim.Point3d(-self.L_run, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self.L_run, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(0.0, self.L_branch, 0.0),
            prim.Point3d(0.0, 1.0, 0.0),
        )
        return self
