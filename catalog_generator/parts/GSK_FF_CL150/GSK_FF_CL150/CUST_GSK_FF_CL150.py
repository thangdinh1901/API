"""Full-face FF gasket with lap-joint stud bolts (visual), ASME B16.5 Class 150.

Part-local axis = +X. FF mating planes at x = 0 and x = T.
Stud OAL catalog = ceil(oal_raw); visual uses oal_raw for exact bearing fit.
Inner nut bearing @ x=-tf-H (west) and x=-tf-H+gap (east); nut body H toward pipe.
"""

import math

import lj_stud_bolts
import pipe_sizes
import primitives as prim
from cl150_rf_flange_dims import CL150_RF_FLANGE_DIMENSIONS
from STUD_BOLTS.StudBolt import StudBolt

DEFAULT_GASKET_THICKNESS_MM = 1.5
DEFAULT_STUD_THREAD_PROTRUSION = 3.0


class GSKFFCL150(prim.ShapeObject):
    """Full-face gasket (OD=O, ID=bore) plus lap-joint stud set."""

    def __init__(self, s, size, thickness=None, pressure_class=150, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        if dn not in CL150_RF_FLANGE_DIMENSIONS:
            raise ValueError(f"No CL150 FF gasket data for DN {dn}.")

        t = (
            DEFAULT_GASKET_THICKNESS_MM
            if thickness is None
            else float(thickness)
        )
        if t <= 0:
            raise ValueError(f"Gasket thickness must be > 0 (got {t}).")

        fd = CL150_RF_FLANGE_DIMENSIONS[dn]
        ring_dims = pipe_sizes.lj_ring_cl150_dims_mm(dn)
        tf = ring_dims["tf"]
        stub_lap_t = pipe_sizes.stubend_lj_a_dims_mm(dn, "long")["T"]

        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.pressure_class = int(pressure_class)
        self.thickness = t
        self.tf = tf
        self.stub_lap_t = stub_lap_t
        self.O = fd["O"]
        self.B = pipe_sizes.pipe_id_sch40_mm(dn)
        self.bcd = fd["bcd"]
        self.n_bolts = fd["n"]

        bolt = lj_stud_bolts.lj_stud_length_mm(dn, pressure_class, gasket_t=t)
        self.bolt_size = bolt["bolt"]
        self.catalog_stud_oal = bolt["L"]
        self.stud_oal = float(bolt["oal_raw"])

        x_west, x_east, grip_geom = lj_stud_bolts.lj_joint_nut_bearings_mm(
            dn, self.bolt_size, gasket_t=t
        )
        self.x_bearing_west = x_west
        self.x_bearing_east = x_east
        self.x_stud_center = t * 0.5
        self.stud_grip_geom = grip_geom
        self.stud_grip_catalog = bolt["grip"]
        x_stud = lj_stud_bolts.lj_stud_start_x_west(
            x_west,
            self.bolt_size,
            thread_protrusion=DEFAULT_STUD_THREAD_PROTRUSION,
        )

        ring = prim.Cylinder(s, diameter=self.O, height=t)
        bore = prim.Cylinder(s, diameter=self.B, height=t + 2.0).move(z=-1.0)
        ring.subtract([bore])
        ring.rotateY(90)

        parts = [ring]
        offset_deg = 360.0 / self.n_bolts / 2.0

        for i in range(self.n_bolts):
            ang = math.radians(i * 360.0 / self.n_bolts + offset_deg)
            hy = (self.bcd / 2.0) * math.cos(ang)
            hz = (self.bcd / 2.0) * math.sin(ang)
            stud = StudBolt(
                s,
                self.bolt_size,
                length=self.stud_oal,
                protruding_threads=DEFAULT_STUD_THREAD_PROTRUSION,
                register_ports=False,
            )
            stud.move(x_stud, hy, hz)
            stud.make_union()
            parts.append(stud)

        body = prim.ShapeAssembly(*parts).make_union()
        super().__init__(body.obj if hasattr(body, "obj") else body)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """FL ports at FF mating planes (west x=0, east x=T)."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self.thickness, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
