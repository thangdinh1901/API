#!/usr/bin/env python3
"""Build Plant 3D geometry from *.scene.json — deployed to CustomScripts/p3d_composer/."""

from __future__ import annotations

import importlib
import json
import re
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


def _skeleton_values(scene: dict[str, Any]) -> dict[str, float]:
    p = scene.get("parameters") or {}
    custom = p.get("customDimensions") or {}
    values: dict[str, float] = {
        "DN": float(p.get("DN") or 0),
        "DN2": float(p.get("DN2") or 0),
        "FaceToFace": float(p.get("FaceToFace") or 0),
        "BodyOD": float(p.get("BodyOD") or 0),
        "ElbowCenterToFace": float(p.get("ElbowCenterToFace") or 0),
        "BodyLength": float(p.get("BodyLength") or 0),
        "BonnetHeight": float(p.get("BonnetHeight") or 0),
        "StemDia": float(p.get("StemDia") or 0),
        "HandwheelOD": float(p.get("HandwheelOD") or 0),
    }
    for key, raw in custom.items():
        try:
            values[str(key)] = float(raw)
        except (TypeError, ValueError):
            continue
    return values


def _eval_skeleton_expr(expr: str, skel: dict[str, float]) -> float | None:
    text = (expr or "").strip()
    if not text:
        return None

    try:
        return float(text)
    except ValueError:
        pass

    lower = {k.lower(): v for k, v in skel.items()}
    if text.lower() in lower:
        return lower[text.lower()]
    if text.lower() == "pipe od from dn":
        return lower.get("bodyod")

    resolved = text
    for name in sorted(skel, key=len, reverse=True):
        resolved = re.sub(
            rf"\b{re.escape(name)}\b",
            str(skel[name]),
            resolved,
            flags=re.IGNORECASE,
        )

    try:
        return float(eval(resolved, {"__builtins__": {}}, {}))  # noqa: S307
    except Exception:
        return None


def _num(node: dict[str, Any], key: str, default: float = 0.0, scene: dict[str, Any] | None = None) -> float:
    params = node.get("parameters") or {}
    entry = params.get(key) or {}
    value = float(entry.get("value", default))
    expr = (entry.get("expression") or "").strip()
    if not expr or scene is None:
        return value

    resolved = _eval_skeleton_expr(expr, _skeleton_values(scene))
    if resolved is None:
        return value

    if value > 0 and abs(value - resolved) > 1e-9:
        return value

    return resolved if resolved > 0 or value <= 0 else value


def _flag(node: dict[str, Any], key: str, default: bool = False) -> bool:
    return _num(node, key, 1.0 if default else 0.0) >= 0.5


import catalog_transforms
importlib.reload(catalog_transforms)
from catalog_transforms import apply_scene_transform as _apply_rotation  # noqa: E402


def _catalog_kwargs(node: dict[str, Any], scene: dict[str, Any] | None = None) -> dict[str, Any]:
    params = node.get("parameters") or {}
    kwargs: dict[str, Any] = {}
    for key in params:
        if key == "DN":
            kwargs["DN"] = int(_num(node, "DN", 100, scene))
        elif key == "DN2":
            kwargs["DN2"] = int(_num(node, "DN2", 80, scene))
        elif key == "CEL":
            val = _num(node, "CEL", 0.0, scene)
            kwargs["CEL"] = None if abs(val) < 1e-9 else float(val)
        else:
            kwargs[key] = _num(node, key, scene=scene)
    if "DN" not in kwargs:
        kwargs["DN"] = int(_num(node, "DN", 100, scene))
    kwargs["preview"] = True
    return kwargs


def _build_catalog_part(s, node: dict[str, Any], scene: dict[str, Any]) -> ShapeObject:
    part_id = node.get("catalogPartId") or ""
    fn_name = f"CUST_{part_id}"
    try:
        mod = importlib.import_module(fn_name)
        builder = getattr(mod, fn_name)
        kwargs = _catalog_kwargs(node, scene)
        print("P3D Composer: catalog %s DN=%s" % (fn_name, kwargs.get("DN")))
        shape = builder(s, **kwargs)
    except Exception as ex:
        raise RuntimeError("P3D Composer: failed to build catalog part %s: %s" % (part_id, ex)) from ex
    return _apply_rotation(_as_shape_object(shape), node)


def _build_part(s, node: dict[str, Any], scene: dict[str, Any]) -> ShapeObject:
    if node.get("catalogPartId"):
        return _build_catalog_part(s, node, scene)

    def n(key: str, default: float = 0.0) -> float:
        return _num(node, key, default, scene)

    ptype = (node.get("type") or "").upper()
    if ptype == "BOX":
        shape = Box(s, n("L"), n("W"), n("H"))
    elif ptype == "CYLINDER":
        d, h = n("D"), n("L")
        o = n("O", d / 2)
        wall = max(0.0, d / 2 - o)
        r2 = n("R2", 0.0)
        ellipse = r2 if r2 > 1e-9 and abs(r2 - d / 2) > 1e-9 else None
        shape = Cylinder(s, diameter=d, height=h, wall_thickness=wall, ellipse_diameter=ellipse)
    elif ptype == "CONE":
        shape = Cone(
            s,
            bottom_diameter=n("D1"),
            height=n("H"),
            top_diameter=n("D2"),
            eccentricity=n("E"),
        )
    elif ptype == "TORUS":
        shape = Torus(s, diameter=n("D"), thickness=n("T"))
    elif ptype == "SPHERE":
        shape = Sphere(s, radius=n("R"))
    elif ptype == "HALFSPHERE":
        shape = HalfSphere(s, radius=n("R"))
    elif ptype == "REDUCED_ELBOW":
        shape = Reduced_elbow(
            s,
            diameter1=n("D"),
            diameter2=n("D2"),
            bend_radius=n("R"),
            angle=n("A", 90),
        )
    elif ptype == "ELBOW":
        shape = Elbow(
            s,
            diameter=n("D"),
            bend_radius=n("R"),
            angle=n("A", 90),
        )
    elif ptype == "SEGMENTED_ELBOW":
        shape = SegmentedElbow(
            s,
            diameter=n("D"),
            bend_radius=n("R"),
            angle=n("A", 90),
            segments=int(n("S", 4)),
        )
    elif ptype == "ELLIPSOID_HEAD":
        shape = EllipsoidHead(s, diameter=n("D"))
    elif ptype == "ELLIPSOID_HEAD2":
        shape = EllipsoidHead2(s, diameter=n("D"))
    elif ptype == "ELLIPSOID_SEGMENT":
        shape = EllipsoidSegment(
            s,
            radius_X=n("RX"),
            radius_Y=n("RY"),
            angle=n("A1"),
            rotation_start=n("A2"),
            angle_start=n("A3"),
            angle_end=n("A4", 360),
        )
    elif ptype == "PYRAMID":
        shape = Pyramid(
            s,
            base_length=n("L"),
            base_width=n("W"),
            frustum_height=n("H"),
            total_height=n("HT"),
        )
    elif ptype == "ROUND_RECTANGLE":
        shape = RoundRectangle(
            s,
            base_length=n("L"),
            base_width=n("W"),
            height=n("H"),
            diam=n("R2") * 2,
            eccentricity=n("E"),
        )
    elif ptype == "SPHERE_SEGMENT":
        shape = SphereSegment(
            s,
            radius=n("R"),
            segment_height=n("H"),
            start_height=n("SH"),
        )
    elif ptype == "TORISPHERIC_HEAD":
        shape = TorisPhericHead(s, diameter=n("D"))
    elif ptype == "TORISPHERIC_HEAD2":
        shape = TorisPhericHead2(s, diameter=n("D"))
    elif ptype == "TORISPHERIC_HEAD_H":
        shape = TorisPhericHeadH(s, diameter=n("D"), height=n("H"))
    elif ptype == "FILLET":
        shape = Fillet(
            s,
            radius=n("R"),
            height=n("H"),
            angle=n("A", 90),
        )
    elif ptype == "CYLINDER_CHAMFERED":
        shape = CylinderChamfered(
            s,
            diameter=n("D"),
            height=n("L"),
            chamfer=n("C"),
            chamfer_angle=n("CA", 45),
            double_chamfer=_flag(node, "DF"),
        )
    elif ptype == "BOX_WITH_FILLET":
        shape = BoxWithFillet(
            s,
            length=n("L"),
            width=n("W"),
            height=n("H"),
            radius=n("R"),
            number_of_fillets=int(n("NF", 4)),
        )
    elif ptype == "CYLINDER_WITH_FILLET":
        shape = CylinderWithFillet(
            s,
            diameter=n("D"),
            height=n("L"),
            fillet_radius=n("FR"),
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
        shapes[node_id] = _build_part(plant_shape, part, scene)
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
