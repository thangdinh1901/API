# Lap-joint CL150 FF stud lengths (mm).
#
# Gap between backing rings (nut bearing to nut bearing):
#   G = T + 2×tf + 2×H
# OAL (round up):
#   L = G + 2×H + 2×(3×pitch) = T + 2×tf + 4×H + 6×pitch

from __future__ import annotations

import math

import pipe_sizes
from STUD_BOLTS import bolting_data

# Bump when nut/stud placement logic changes (checked by deploy_manifest / verify_lj_deploy).
LJ_STUD_PLACEMENT_REV = "2026-06-11-tf-H"

DEFAULT_GASKET_THICKNESS_MM = 1.5
DEFAULT_STUD_THREAD_PROTRUSION = 3.0

_NUT_H_MM = {
    "1/2": 12.303,
    "5/8": 15.478,
    "3/4": 18.653,
    "7/8": 21.828,
    "1": 25.003,
    "1-1/8": 28.178,
}

_NUT_P_MM = {
    "1/2": 1.954,
    "5/8": 2.309,
    "3/4": 2.540,
    "7/8": 2.822,
    "1": 3.175,
    "1-1/8": 3.175,
}


def nut_thickness_mm(bolt_size: str) -> float:
    key = bolt_size.strip().strip('"')
    return _NUT_H_MM.get(key, 15.478)


def nut_pitch_mm(bolt_size: str) -> float:
    key = bolt_size.strip().strip('"')
    return _NUT_P_MM.get(key, 2.309)


def stud_bearing_inset_mm(bolt_size: str) -> float:
    """H + 3× pitch (one end), mm."""
    return nut_thickness_mm(bolt_size) + DEFAULT_STUD_THREAD_PROTRUSION * nut_pitch_mm(
        bolt_size
    )


def lj_stud_grip_mm(
    dn: int,
    bolt_size: str,
    *,
    gasket_t: float = DEFAULT_GASKET_THICKNESS_MM,
) -> float:
    """Gap between backing rings (inner nut bearing to inner nut bearing), mm."""
    ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
    h = nut_thickness_mm(bolt_size)
    return float(gasket_t) + 2.0 * float(ring["tf"]) + 2.0 * h


def lj_joint_nut_bearings_mm(
    dn: int,
    bolt_size: str,
    *,
    gasket_t: float = DEFAULT_GASKET_THICKNESS_MM,
) -> tuple[float, float, float]:
    """Inner nut bearing planes along joint +X.

    West ring (pipe -X): plate x in [-tf, 0], pipe-side face at x = -tf.
    Inner nut bearing at x = -tf - H (one H further toward pipe).
    """
    ring = pipe_sizes.lj_ring_cl150_dims_mm(dn)
    tf = float(ring["tf"])
    h = nut_thickness_mm(bolt_size)
    grip = lj_stud_grip_mm(dn, bolt_size, gasket_t=gasket_t)
    x_west = -tf - h
    x_east = x_west + grip
    return x_west, x_east, grip


def lj_stud_start_x_west(
    x_west: float,
    bolt_size: str,
    *,
    thread_protrusion: float = DEFAULT_STUD_THREAD_PROTRUSION,
) -> float:
    """Min-X of stud so inner nut bearing sits on west ring pipe-side face."""
    return x_west - stud_bearing_inset_mm(bolt_size)


def lj_stud_length_mm(
    dn: int,
    pressure_class: int = 150,
    *,
    gasket_t: float = DEFAULT_GASKET_THICKNESS_MM,
) -> dict:
    """Return bolt size, count, OAL (ceil mm), and grip for lap-joint FF joints."""
    rf = bolting_data.lookup(pressure_class, dn=dn, face=bolting_data.FaceType.RF)
    bolt = rf["bolt"]
    grip = lj_stud_grip_mm(dn, bolt, gasket_t=gasket_t)
    inset = stud_bearing_inset_mm(bolt)
    pitch = nut_pitch_mm(bolt)
    protrusion = DEFAULT_STUD_THREAD_PROTRUSION * pitch
    oal = grip + 2.0 * inset
    return {
        "bolt": bolt,
        "n": rf["n"],
        "L": int(math.ceil(oal)),
        "grip": grip,
        "oal_raw": oal,
        "protrusion_mm": protrusion,
        "protruding_threads": DEFAULT_STUD_THREAD_PROTRUSION,
        "nps": rf["nps"],
    }
