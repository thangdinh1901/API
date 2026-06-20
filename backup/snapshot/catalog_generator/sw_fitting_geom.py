"""ASME B16.11 Class 3000 socket-weld forging helpers.

Solid forging collar (OD B+2C) with socket bore B × J cut by boolean subtract.
Port at outer face L = A + J.
"""

from __future__ import annotations

import math
from typing import Sequence

import pipe_sizes
import primitives as prim

SocketPort = tuple[int, prim.Point3d, prim.Point3d]

# Extra cutter length (mm) so subtract clears the inner shoulder cleanly.
_BORE_CUT_OVERLAP_MM = 1.0


def _normalize(dx: float, dy: float, dz: float) -> tuple[float, float, float]:
    mag = math.sqrt(dx * dx + dy * dy + dz * dz)
    if mag < 1e-9:
        raise ValueError("Zero direction vector")
    return dx / mag, dy / mag, dz / mag


def _apply_collar_transform(
    shape: prim.ShapeObject,
    port: prim.Point3d,
    outward: prim.Point3d,
    length: float,
) -> prim.ShapeObject:
    """Orient +Z cylinder and place forging from inner shoulder to port face."""
    ox, oy, oz = _normalize(outward.x, outward.y, outward.z)

    if abs(oz) < 1e-6 and abs(oy) < 1e-6:
        shape = shape.rotateY(-90) if ox > 0 else shape.rotateY(90)
        shape.move(x=port.x)
    elif abs(oz) < 1e-6 and abs(ox) < 1e-6:
        shape = shape.rotateX(90) if oy > 0 else shape.rotateX(-90)
        shape.move(y=port.y)
    elif abs(oz) < 1e-6:
        angle = math.degrees(math.atan2(oy, ox))
        shape = shape.rotateZ(angle).rotateY(90)
        shape.move(
            x=port.x - ox * length,
            y=port.y - oy * length,
            z=port.z - oz * length,
        )
    else:
        raise ValueError(f"Unsupported port direction ({ox}, {oy}, {oz})")

    return shape


def _place_socket_on_port(
    s,
    port: prim.Point3d,
    outward: prim.Point3d,
    *,
    outer_d: float,
    bore_d: float,
    length: float,
) -> prim.ShapeObject:
    """Solid forging J mm with socket bore B mm subtracted (B16.11)."""
    forging = prim.Cylinder(s, diameter=outer_d, height=length)
    forging = _apply_collar_transform(forging, port, outward, length)

    cut_len = length + _BORE_CUT_OVERLAP_MM
    bore = prim.Cylinder(s, diameter=bore_d, height=cut_len)
    bore = _apply_collar_transform(bore, port, outward, length)

    forging.subtract(bore)
    return forging


def socket_ring(
    s,
    dn: int,
    port: prim.Point3d,
    outward: prim.Point3d,
) -> prim.ShapeObject:
    forge_od = pipe_sizes.sw_cl3000_forging_od_mm(dn)
    bore = pipe_sizes.sw_cl3000_socket_bore_mm(dn)
    depth = pipe_sizes.sw_cl3000_socket_depth_mm(dn)

    return _place_socket_on_port(
        s,
        port,
        outward,
        outer_d=forge_od,
        bore_d=bore,
        length=depth,
    )


def elbow_socket_collar(
    s,
    dn: int,
    port: prim.Point3d,
    outward: prim.Point3d,
) -> prim.ShapeObject:
    forge_od = pipe_sizes.sw_cl3000_forging_od_mm(dn)
    bore = pipe_sizes.sw_cl3000_socket_bore_mm(dn)
    depth = pipe_sizes.sw_cl3000_socket_depth_mm(dn)

    return _place_socket_on_port(
        s,
        port,
        outward,
        outer_d=forge_od,
        bore_d=bore,
        length=depth,
    )


def apply_socket_rings(
    body: prim.ShapeObject,
    s,
    ports: Sequence[SocketPort],
) -> prim.ShapeObject:
    rings = [socket_ring(s, dn, pt, out) for dn, pt, out in ports]
    if rings:
        body.combine(rings)
    return body


def apply_elbow_socket_collars(
    body: prim.ShapeObject,
    s,
    ports: Sequence[SocketPort],
) -> prim.ShapeObject:
    collars = [elbow_socket_collar(s, dn, pt, out) for dn, pt, out in ports]
    if collars:
        body.combine(collars)
    return body
