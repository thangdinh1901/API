"""Scene-graph transforms for catalog parts (bake frame, jogs, WCS move).

Plant 3D rotateX/Y/Z pivots at WCS (0,0,0). Catalog CUST_*.py often bakes rotateY(90);
scene JSON stores WCS origin and jogs — replay undoes bake, applies ops in build frame, redoes bake.
"""

from __future__ import annotations

import math
from typing import Any

from primitives import ShapeObject  # type: ignore

_IDENTITY = [1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0]


def is_identity_rotation(rot: list[float]) -> bool:
    if len(rot) < 9:
        return True
    e = 1e-6
    return (
        abs(rot[0] - 1) < e and abs(rot[4] - 1) < e and abs(rot[8] - 1) < e
        and abs(rot[1]) < e and abs(rot[2]) < e and abs(rot[3]) < e
        and abs(rot[5]) < e and abs(rot[6]) < e and abs(rot[7]) < e
    )


def axis_matrix(axis: str, rad: float) -> list[float]:
    c, s = math.cos(rad), math.sin(rad)
    ax = axis.upper()
    if ax == "X":
        return [1, 0, 0, 0, c, -s, 0, s, c]
    if ax == "Y":
        return [c, 0, s, 0, 1, 0, -s, 0, c]
    if ax == "Z":
        return [c, -s, 0, s, c, 0, 0, 0, 1]
    raise ValueError(f"Unknown axis '{axis}'")


def invert3(m: list[float]) -> list[float]:
    return [m[0], m[3], m[6], m[1], m[4], m[7], m[2], m[5], m[8]]


def mul3(a: list[float], b: list[float]) -> list[float]:
    out = [0.0] * 9
    for i in range(3):
        for j in range(3):
            out[i * 3 + j] = sum(a[i * 3 + k] * b[k * 3 + j] for k in range(3))
    return out


def mul3_vec(m: list[float], vx: float, vy: float, vz: float) -> tuple[float, float, float]:
    return (
        m[0] * vx + m[1] * vy + m[2] * vz,
        m[3] * vx + m[4] * vy + m[5] * vz,
        m[6] * vx + m[7] * vy + m[8] * vz,
    )


def catalog_frame_rotation(node: dict[str, Any]) -> list[float]:
    raw = node.get("catalogFrameRotation")
    if isinstance(raw, list) and len(raw) >= 9:
        return [float(raw[i]) for i in range(9)]
    return list(_IDENTITY)


def world_vector_to_build(
    r_cat: list[float], wx: float, wy: float, wz: float
) -> tuple[float, float, float]:
    """WCS translation → build frame (geometry is undo-baked)."""
    if is_identity_rotation(r_cat):
        return wx, wy, wz
    return mul3_vec(invert3(r_cat), wx, wy, wz)


def conj_to_build_frame(r_cat: list[float], delta: list[float]) -> list[float]:
    """inv(R_cat) * delta * R_cat — map axis rotation into build frame."""
    if is_identity_rotation(r_cat):
        return delta
    return mul3(mul3(invert3(r_cat), delta), r_cat)


def undo_catalog_bake(shape: ShapeObject, r_cat: list[float]) -> ShapeObject:
    if is_identity_rotation(r_cat):
        return shape
    return shape.apply_matrix_rotation(invert3(r_cat))


def redo_catalog_bake(shape: ShapeObject, r_cat: list[float]) -> ShapeObject:
    if is_identity_rotation(r_cat):
        return shape
    return shape.apply_matrix_rotation(r_cat)


def _apply_local_jog(shape: ShapeObject, axis: str, degrees: float) -> ShapeObject:
    ax = axis.upper()[:1]
    if ax == "X":
        return shape.rotateX(degrees)
    if ax == "Y":
        return shape.rotateY(degrees)
    if ax == "Z":
        return shape.rotateZ(degrees)
    raise ValueError(f"Unknown axis '{axis}'")


def apply_axis_jog(
    shape: ShapeObject, r_cat: list[float], axis: str, degrees: float
) -> ShapeObject:
    """One jog about axis (WCS for world jogs, connection frame for object jogs)."""
    delta = axis_matrix(axis, math.radians(degrees))
    if is_identity_rotation(r_cat):
        return _apply_local_jog(shape, axis, degrees)
    return shape.apply_matrix_rotation(conj_to_build_frame(r_cat, delta))


def apply_axis_jog_at_pivot(
    shape: ShapeObject,
    r_cat: list[float],
    axis: str,
    degrees: float,
    px: float,
    py: float,
    pz: float,
) -> ShapeObject:
    """Object jog in place — pivot because Plant3D rotates about WCS origin."""
    if abs(px) > 1e-9 or abs(py) > 1e-9 or abs(pz) > 1e-9:
        shape.move(x=-px, y=-py, z=-pz)
    shape = apply_axis_jog(shape, r_cat, axis, degrees)
    if abs(px) > 1e-9 or abs(py) > 1e-9 or abs(pz) > 1e-9:
        shape.move(x=px, y=py, z=pz)
    return shape


def is_world_jog(jog: dict[str, Any]) -> bool:
    return jog.get("world") is True


def matrix_from_jogs(jogs: list[dict[str, Any]]) -> list[float]:
    r = list(_IDENTITY)
    for jog in jogs:
        axis = str(jog.get("axis", "Z")).upper()[:1]
        deg = float(jog.get("degrees", 0.0))
        r = mul3(r, axis_matrix(axis, math.radians(deg)))
    return r


def apply_scene_transform(shape: ShapeObject, node: dict[str, Any]) -> ShapeObject:
    """Replay origin + rotation jogs from scene JSON onto a built shape."""
    jogs = node.get("rotationJogs") or []
    r_cat = catalog_frame_rotation(node)
    has_cat = not is_identity_rotation(r_cat)

    origin = node.get("origin") or [0, 0, 0]
    ox, oy, oz = (float(origin[0]), float(origin[1]), float(origin[2]))
    has_move = abs(ox) > 1e-9 or abs(oy) > 1e-9 or abs(oz) > 1e-9

    r_scene = node.get("rotation") or list(_IDENTITY)
    has_jogs = len(jogs) > 0
    if has_jogs:
        r_scene = matrix_from_jogs(jogs)
    has_rot = len(r_scene) == 9 and not is_identity_rotation(r_scene)

    if not has_jogs and not has_rot and not has_move:
        return shape

    mx, my, mz = world_vector_to_build(r_cat, ox, oy, oz) if has_cat else (ox, oy, oz)

    if has_jogs:
        n_world = sum(1 for j in jogs if is_world_jog(j))
        print(
            "P3D Composer: replay %d jog(s) (%d world, %d object), origin (%.3f, %.3f, %.3f)"
            % (len(jogs), n_world, len(jogs) - n_world, ox, oy, oz)
        )
    elif has_rot or has_move:
        print("P3D Composer: transform — origin (%.3f, %.3f, %.3f)" % (ox, oy, oz))

    if has_cat:
        shape = undo_catalog_bake(shape, r_cat)

    if has_jogs:
        placed = False
        for jog in jogs:
            axis = str(jog.get("axis", "Z")).upper()[:1]
            deg = float(jog.get("degrees", 0.0))
            if is_world_jog(jog):
                if placed:
                    shape.move(x=-mx, y=-my, z=-mz)
                    placed = False
                shape = apply_axis_jog(shape, r_cat, axis, deg)
            else:
                if not placed and has_move:
                    shape.move(x=mx, y=my, z=mz)
                    placed = True
                px, py, pz = (mx, my, mz) if placed else (0.0, 0.0, 0.0)
                shape = apply_axis_jog_at_pivot(shape, r_cat, axis, deg, px, py, pz)

        if has_move and not placed:
            shape.move(x=mx, y=my, z=mz)
    else:
        if has_move:
            shape.move(x=mx, y=my, z=mz)
        if has_rot:
            shape.apply_matrix_rotation(r_scene)

    if has_cat:
        shape = redo_catalog_bake(shape, r_cat)

    return shape
