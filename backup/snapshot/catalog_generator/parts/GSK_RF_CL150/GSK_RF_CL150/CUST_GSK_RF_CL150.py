"""RF gasket with stud bolts (visual joint assembly), ASME B16.5 Class 150.

Part-local connection axis = **+X** (not world East/West). Port 0 normal −X, port 1
normal +X. Plant 3D applies the placement transform when the part is inserted, so
the same script works for pipe runs along North, South, vertical, etc.; studs and
bolt circle are fixed in part space and rotate with the gasket.

Gasket body spans x = 0 .. T. RF mating planes at x = 0 (port 0) and x = T (port 1).

WN flange (after rotateY(90)): RF height ``rf_h`` at the mating tip, disc ``tf``
behind it toward the hub. Outer nut face per flange: ``rf_h + tf`` from RF tip.

Nut bearing faces (part-local x):
  west  = -(rf_h + tf)     (west WN, RF tip at x = 0)
  east  = T + rf_h + tf    (east WN, RF tip at x = T)
  grip  = T + 2*(rf_h + tf)

Use ``build_cl150_rf_joint()`` for hot-reload / preview so gasket local = world
without an extra −T shift on the gasket alone (that caused a T nut misalignment).
"""

import math

import pipe_sizes
import primitives as prim
from STUD_BOLTS import bolting_data
from STUD_BOLTS.StudBolt import StudBolt
from CUST_WN_FLRF_CL150 import WNFLRFCL150, RAISED_FACE_HEIGHT

DEFAULT_GASKET_THICKNESS_MM = 1.5
DEFAULT_STUD_THREAD_PROTRUSION = 3.0


def _stud_bearing_inset(bolt_size):
    d = StudBolt.DIMENSIONS[bolt_size]
    protrusion = DEFAULT_STUD_THREAD_PROTRUSION * d["P"]
    return protrusion + d["H"], protrusion


def _joint_nut_bearings(t, rf_h, tf):
    half_flange = rf_h + tf
    return -half_flange, t + half_flange, t + 2.0 * half_flange


def build_cl150_rf_joint(
    s,
    dn,
    thickness=None,
    pressure_class=150,
    wn_color=50,
    gsk_color=30,
):
    """WN | gasket+studs | WN along +X for visual stud-length check.

    World/part frame (pipe toward +X):
      west WN RF at x=0, gasket 0..T, east WN RF at x=T.
    """
    t = (
        DEFAULT_GASKET_THICKNESS_MM
        if thickness is None
        else float(thickness)
    )
    joint = GSKRFCL150(
        s, dn, thickness=t, pressure_class=pressure_class
    ).set_color(gsk_color)
    wn_w = WNFLRFCL150(s, dn).rotateY(180).set_color(wn_color)
    wn_e = WNFLRFCL150(s, dn).move(x=t).set_color(wn_color)
    return prim.ShapeAssembly(wn_w, joint, wn_e)


class GSKRFCL150(prim.ShapeObject):
    """Ring gasket (OD=G, ID=B) plus a full stud set for visual joint check."""

    def __init__(self, s, size, thickness=None, pressure_class=150, *, add_ports=True):
        dn = pipe_sizes.resolve_dn(size)
        if dn not in WNFLRFCL150.DIMENSIONS:
            raise ValueError(f"No CL150 RF gasket data for DN {dn}.")

        t = (
            DEFAULT_GASKET_THICKNESS_MM
            if thickness is None
            else float(thickness)
        )
        if t <= 0:
            raise ValueError(f"Gasket thickness must be > 0 (got {t}).")

        fd = WNFLRFCL150.DIMENSIONS[dn]
        rf_h = RAISED_FACE_HEIGHT
        tf = fd["tf"]

        self.dn = dn
        self.nps = pipe_sizes.dn_to_nps(dn)
        self.pressure_class = int(pressure_class)
        self.thickness = t
        self.rf_h = rf_h
        self.tf = tf
        self.G = fd["G"]
        self.B = fd["B"]
        self.bcd = fd["bcd"]
        self.n_bolts = fd["n"]

        bolt = bolting_data.lookup(
            self.pressure_class, dn=dn, face=bolting_data.FaceType.RF
        )
        self.bolt_size = bolt["bolt"]
        self.catalog_stud_oal = bolt["L"]

        bearing_inset, _ = _stud_bearing_inset(self.bolt_size)
        x_west, x_east, grip_geom = _joint_nut_bearings(t, rf_h, tf)
        self.x_bearing_west = x_west
        self.x_bearing_east = x_east
        self.x_stud_center = t * 0.5
        self.stud_grip_geom = grip_geom
        self.stud_grip_catalog = self.catalog_stud_oal - 2.0 * bearing_inset
        self.stud_oal = grip_geom + 2.0 * bearing_inset
        x_stud = x_west - bearing_inset

        ring = prim.Cylinder(s, diameter=self.G, height=t)
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
        """FL ports at RF mating planes (west x=0, east x=T)."""
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
