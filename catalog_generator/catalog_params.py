"""Resolve catalog geometry parameters passed from Plant 3D custom-script invocation."""

from __future__ import annotations

from typing import Any

import pipe_sizes


def _coerce_number(value: Any) -> float | None:
    if value is None or value == "":
        return None
    if hasattr(value, "value"):
        value = value.value
    if isinstance(value, bool):
        return None
    if isinstance(value, (int, float)):
        return float(value)
    text = str(value).strip()
    if not text:
        return None
    try:
        return float(text)
    except ValueError:
        return None


def _parse_assignment_blob(blob: str) -> dict[str, float]:
    out: dict[str, float] = {}
    for part in str(blob).split(","):
        part = part.strip()
        if "=" not in part:
            continue
        name, raw = part.split("=", 1)
        num = _coerce_number(raw.strip())
        if num is not None:
            out[name.strip().upper()] = num
    return out


def _gather_assignment_maps(kw: dict[str, Any]) -> list[dict[str, float]]:
    maps: list[dict[str, float]] = []
    for key in (
        "ContentGeometryParamDefinition",
        "ParamDefinition",
        "Parameters",
        "params",
        "PARAMS",
    ):
        val = kw.get(key)
        if isinstance(val, str) and "=" in val:
            maps.append(_parse_assignment_blob(val))
        elif isinstance(val, dict):
            parsed: dict[str, float] = {}
            for k, v in val.items():
                num = _coerce_number(v)
                if num is not None:
                    parsed[str(k).upper()] = num
            if parsed:
                maps.append(parsed)
    return maps


def resolve_catalog_dn(default_dn: Any = 100, **kw: Any) -> int:
    """DN (mm) from Plant kwargs, assignment string, or NominalDiameter."""
    if isinstance(default_dn, str):
        blob = default_dn.strip()
        if "=" in blob:
            amap = _parse_assignment_blob(blob)
            if "DN" in amap:
                try:
                    return pipe_sizes.resolve_dn(int(round(amap["DN"])))
                except (ValueError, TypeError):
                    pass
        num = _coerce_number(blob)
        if num is not None:
            try:
                return pipe_sizes.resolve_dn(int(round(num)))
            except (ValueError, TypeError):
                pass

    candidates: list[float] = []

    for key in ("DN", "D", "NominalDiameter", "NOMINALDIAMETER", "SIZE", "Size"):
        num = _coerce_number(kw.get(key))
        if num is not None:
            candidates.append(num)

    for val in kw.values():
        if isinstance(val, str) and "DN=" in val.upper():
            for amap in (_parse_assignment_blob(val),):
                if "DN" in amap:
                    candidates.append(amap["DN"])

    for amap in _gather_assignment_maps(kw):
        if "DN" in amap:
            candidates.append(amap["DN"])

    pos = _coerce_number(default_dn)
    if pos is not None:
        candidates.append(pos)

    if candidates:
        explicit = candidates[:-1] if len(candidates) > 1 else candidates
        for num in explicit or candidates:
            try:
                return pipe_sizes.resolve_dn(int(round(num)))
            except (ValueError, TypeError):
                continue
        try:
            return pipe_sizes.resolve_dn(int(round(candidates[-1])))
        except (ValueError, TypeError):
            pass

    try:
        return pipe_sizes.resolve_dn(default_dn)
    except (ValueError, TypeError):
        return pipe_sizes.resolve_dn(100)


def resolve_catalog_float(
    name: str,
    default: Any,
    *,
    default_value: float | None = None,
    **kw: Any,
) -> float:
    """Resolve a numeric catalog parameter (e.g. T, CEL)."""
    key = name.upper()
    for amap in _gather_assignment_maps(kw):
        if key in amap:
            return float(amap[key])

    num = _coerce_number(kw.get(name, kw.get(key, default)))
    if num is not None:
        return float(num)
    if default_value is not None:
        return float(default_value)
    return float(default)
