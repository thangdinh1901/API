#!/usr/bin/env python3
"""Build Plant 3D geometry from *.scene.json — deployed to CustomScripts/p3d_composer/."""

from __future__ import annotations

import importlib
import json
import sys
from pathlib import Path
from typing import Any
from uuid import UUID

SCRIPT_DIR = Path(__file__).resolve().parent
CUSTOM_SCRIPTS = SCRIPT_DIR.parent

for _candidate in (CUSTOM_SCRIPTS, SCRIPT_DIR.parent.parent / "Plant3DSkeletonManager"):
    if (_candidate / "primitives.py").exists() and str(_candidate) not in sys.path:
        sys.path.insert(0, str(_candidate))

if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from primitives import (  # type: ignore
    Box,
    BoxWithFillet,
    Cone,
    Cylinder,
    CylinderChamfered,
    CylinderWithFillet,
    Elbow,
    EllipsoidHead,
    EllipsoidHead2,
    EllipsoidSegment,
    Fillet,
    HalfSphere,
    Pyramid,
    Reduced_elbow,
    RoundRectangle,
    SegmentedElbow,
    ShapeAssembly,
    ShapeObject,
    Sphere,
    SphereSegment,
    TorisPhericHead,
    TorisPhericHead2,
    TorisPhericHeadH,
    Torus,
)


def _as_shape_object(shape) -> ShapeObject:
    """Always return a primitives.ShapeObject wrapper (catalog may return subclasses or raw p3dprimitive)."""
    if isinstance(shape, ShapeObject):
        return shape
    if hasattr(shape, "obj"):
        return ShapeObject(shape.obj)
    return ShapeObject(shape)


def _num(node: dict[str, Any], key: str, default: float = 0.0) -> float:
    params = node.get("parameters") or {}
    entry = params.get(key) or {}
    return float(entry.get("value", default))


def _flag(node: dict[str, Any], key: str, default: bool = False) -> bool:
    return _num(node, key, 1.0 if default else 0.0) >= 0.5


import catalog_transforms
importlib.reload(catalog_transforms)
from catalog_transforms import apply_scene_transform as _apply_rotation  # noqa: E402


def _catalog_kwargs(node: dict[str, Any]) -> dict[str, Any]:
    params = node.get("parameters") or {}
    kwargs: dict[str, Any] = {}
    for key in params:
        if key == "DN":
            kwargs["DN"] = int(_num(node, "DN", 100))
        elif key == "DN2":
            kwargs["DN2"] = int(_num(node, "DN2", 80))
        elif key == "CEL":
            val = _num(node, "CEL", 0.0)
            kwargs["CEL"] = None if abs(val) < 1e-9 else float(val)
        else:
            kwargs[key] = _num(node, key)
    if "DN" not in kwargs:
        kwargs["DN"] = int(_num(node, "DN", 100))
    kwargs["preview"] = True
    return kwargs


def _build_catalog_part(s, node: dict[str, Any]) -> ShapeObject:
    part_id = node.get("catalogPartId") or ""
    fn_name = f"CUST_{part_id}"
    try:
        mod = importlib.import_module(fn_name)
        builder = getattr(mod, fn_name)
        kwargs = _catalog_kwargs(node)
        print("P3D Composer: catalog %s DN=%s" % (fn_name, kwargs.get("DN")))
        shape = builder(s, **kwargs)
    except Exception as ex:
        raise RuntimeError("P3D Composer: failed to build catalog part %s: %s" % (part_id, ex)) from ex
    return _apply_rotation(_as_shape_object(shape), node)


def _build_part(s, node: dict[str, Any]) -> ShapeObject:
    if node.get("catalogPartId"):
        return _build_catalog_part(s, node)

    ptype = (node.get("type") or "").upper()
    if ptype == "BOX":
        shape = Box(s, _num(node, "L"), _num(node, "W"), _num(node, "H"))
    elif ptype == "CYLINDER":
        d, h = _num(node, "D"), _num(node, "L")
        o = _num(node, "O", d / 2)
        wall = max(0.0, d / 2 - o)
        r2 = _num(node, "R2", 0.0)
        ellipse = r2 if r2 > 1e-9 and abs(r2 - d / 2) > 1e-9 else None
        shape = Cylinder(s, diameter=d, height=h, wall_thickness=wall, ellipse_diameter=ellipse)
    elif ptype == "CONE":
        shape = Cone(
            s,
            bottom_diameter=_num(node, "D1"),
            height=_num(node, "H"),
            top_diameter=_num(node, "D2"),
            eccentricity=_num(node, "E"),
        )
    elif ptype == "TORUS":
        shape = Torus(s, diameter=_num(node, "D"), thickness=_num(node, "T"))
    elif ptype == "SPHERE":
        shape = Sphere(s, radius=_num(node, "R"))
    elif ptype == "HALFSPHERE":
        shape = HalfSphere(s, radius=_num(node, "R"))
    elif ptype == "REDUCED_ELBOW":
        shape = Reduced_elbow(
            s,
            diameter1=_num(node, "D"),
            diameter2=_num(node, "D2"),
            bend_radius=_num(node, "R"),
            angle=_num(node, "A", 90),
        )
    elif ptype == "ELBOW":
        shape = Elbow(
            s,
            diameter=_num(node, "D"),
            bend_radius=_num(node, "R"),
            angle=_num(node, "A", 90),
        )
    elif ptype == "SEGMENTED_ELBOW":
        shape = SegmentedElbow(
            s,
            diameter=_num(node, "D"),
            bend_radius=_num(node, "R"),
            angle=_num(node, "A", 90),
            segments=int(_num(node, "S", 4)),
        )
    elif ptype == "ELLIPSOID_HEAD":
        shape = EllipsoidHead(s, diameter=_num(node, "D"))
    elif ptype == "ELLIPSOID_HEAD2":
        shape = EllipsoidHead2(s, diameter=_num(node, "D"))
    elif ptype == "ELLIPSOID_SEGMENT":
        shape = EllipsoidSegment(
            s,
            radius_X=_num(node, "RX"),
            radius_Y=_num(node, "RY"),
            angle=_num(node, "A1"),
            rotation_start=_num(node, "A2"),
            angle_start=_num(node, "A3"),
            angle_end=_num(node, "A4", 360),
        )
    elif ptype == "PYRAMID":
        shape = Pyramid(
            s,
            base_length=_num(node, "L"),
            base_width=_num(node, "W"),
            frustum_height=_num(node, "H"),
            total_height=_num(node, "HT"),
        )
    elif ptype == "ROUND_RECTANGLE":
        shape = RoundRectangle(
            s,
            base_length=_num(node, "L"),
            base_width=_num(node, "W"),
            height=_num(node, "H"),
            diam=_num(node, "R2") * 2,
            eccentricity=_num(node, "E"),
        )
    elif ptype == "SPHERE_SEGMENT":
        shape = SphereSegment(
            s,
            radius=_num(node, "R"),
            segment_height=_num(node, "H"),
            start_height=_num(node, "SH"),
        )
    elif ptype == "TORISPHERIC_HEAD":
        shape = TorisPhericHead(s, diameter=_num(node, "D"))
    elif ptype == "TORISPHERIC_HEAD2":
        shape = TorisPhericHead2(s, diameter=_num(node, "D"))
    elif ptype == "TORISPHERIC_HEAD_H":
        shape = TorisPhericHeadH(s, diameter=_num(node, "D"), height=_num(node, "H"))
    elif ptype == "FILLET":
        shape = Fillet(
            s,
            radius=_num(node, "R"),
            height=_num(node, "H"),
            angle=_num(node, "A", 90),
        )
    elif ptype == "CYLINDER_CHAMFERED":
        shape = CylinderChamfered(
            s,
            diameter=_num(node, "D"),
            height=_num(node, "L"),
            chamfer=_num(node, "C"),
            chamfer_angle=_num(node, "CA", 45),
            double_chamfer=_flag(node, "DF"),
        )
    elif ptype == "BOX_WITH_FILLET":
        shape = BoxWithFillet(
            s,
            length=_num(node, "L"),
            width=_num(node, "W"),
            height=_num(node, "H"),
            radius=_num(node, "R"),
            number_of_fillets=int(_num(node, "NF", 4)),
        )
    elif ptype == "CYLINDER_WITH_FILLET":
        shape = CylinderWithFillet(
            s,
            diameter=_num(node, "D"),
            height=_num(node, "L"),
            fillet_radius=_num(node, "FR"),
            double_fillet=_flag(node, "DF"),
        )
    else:
        raise ValueError(f"Unsupported primitive type: {ptype}")

    return _apply_rotation(shape, node)


def _apply_booleans(shapes: dict[UUID, ShapeObject], operations: list[dict[str, Any]]) -> None:
    for op in sorted(operations, key=lambda o: int(o.get("order", 0))):
        target_id = UUID(str(op["target"]))
        tool_ids = [UUID(str(t)) for t in op.get("tools") or []]
        target = shapes[target_id]
        tools = [shapes[t] for t in tool_ids]
        kind = (op.get("type") or "").upper()
        if kind == "UNION":
            target.combine(tools)
        elif kind == "SUBTRACT":
            target.subtract(tools)
        elif kind == "INTERSECT":
            if len(tools) != 1:
                raise ValueError("INTERSECT requires exactly one tool node.")
            shapes[target_id] = target.intersect(tools[0])
        else:
            raise ValueError(f"Unknown boolean type: {kind}")


def build_scene(scene: dict[str, Any], plant_shape) -> dict[UUID, ShapeObject]:
    shapes: dict[UUID, ShapeObject] = {}
    for part in scene.get("parts") or []:
        node_id = UUID(str(part["id"]))
        shapes[node_id] = _build_part(plant_shape, part)
    _apply_booleans(shapes, scene.get("operations") or [])
    return shapes


def build_combined_scene(scene: dict[str, Any], plant_shape) -> ShapeObject:
    """Build all parts; keep multiple bodies in a ShapeAssembly (same as GV build_preview)."""
    shapes = build_scene(scene, plant_shape)
    parts = [_as_shape_object(p) for p in shapes.values()]
    if not parts:
        raise RuntimeError("P3D Composer: scene has no parts")
    if len(parts) == 1:
        return parts[0]
    return ShapeAssembly(*parts)


def load_scene(path) -> dict[str, Any]:
    with open(path, encoding="utf-8") as f:
        return json.load(f)
