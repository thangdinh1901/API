import math
import re
from enum import Enum

import primitives as prim


# ISO metric hex nut (max dimensions): AC = across corners, thk, AF = across flats.
ISO_DIMENSIONS_MM = {
    "M8":  {"ac": 14.38, "thk": 6.8,  "af": 13.0},
    "M10": {"ac": 17.77, "thk": 8.4,  "af": 16.0},
    "M12": {"ac": 20.03, "thk": 10.8, "af": 18.0},
    "M14": {"ac": 23.35, "thk": 12.8, "af": 21.0},
    "M16": {"ac": 26.75, "thk": 14.8, "af": 24.0},
    "M18": {"ac": 29.56, "thk": 15.8, "af": 27.0},
    "M20": {"ac": 32.95, "thk": 18.0,  "af": 30.0},
    "M22": {"ac": 37.29, "thk": 19.4,  "af": 34.0},
    "M24": {"ac": 39.55, "thk": 21.5,  "af": 36.0},
    "M27": {"ac": 45.20, "thk": 23.8,  "af": 41.0},
    "M30": {"ac": 50.85, "thk": 26.6,  "af": 46.0},
    "M33": {"ac": 55.37, "thk": 28.7,  "af": 50.0},
    "M36": {"ac": 60.79, "thk": 31.0,  "af": 55.0},
    "M39": {"ac": 66.44, "thk": 33.4,  "af": 60.0},
    "M42": {"ac": 71.30, "thk": 34.0,  "af": 65.0},
    "M45": {"ac": 76.95, "thk": 36.0,  "af": 70.0},
    "M48": {"ac": 82.60, "thk": 38.0,  "af": 75.0},
    "M52": {"ac": 88.25, "thk": 42.0,  "af": 80.0},
    "M56": {"ac": 93.56, "thk": 45.0,  "af": 85.0},
    "M60": {"ac": 99.21, "thk": 48.0,  "af": 90.0},
    "M64": {"ac": 104.86, "thk": 51.0, "af": 95.0},
    "M68": {"ac": 110.51, "thk": 54.0, "af": 100.0},
}

# ISO metric coarse thread pitch (mm) for chamfer sizing.
COARSE_PITCH_MM = {
    8: 1.25, 10: 1.5, 12: 1.75, 14: 2.0, 16: 2.0, 18: 2.5, 20: 2.5,
    22: 2.5, 24: 3.0, 27: 3.0, 30: 3.5, 33: 3.5, 36: 4.0, 39: 4.0,
    42: 4.5, 45: 4.5, 48: 5.0, 52: 5.0, 56: 5.5, 60: 5.5, 64: 6.0, 68: 6.0,
}

BORE_CLEARANCE_MM = 0.5


def normalize_designation(designation) -> str:
    """Accept Size enum, 'M12', or 'M 12' -> 'M12'."""
    if hasattr(designation, "value"):
        designation = designation.value
    s = re.sub(r"\s+", "", str(designation).strip().upper())
    if not s.startswith("M"):
        s = "M" + s
    return s


def thread_diameter_mm(designation: str) -> float:
    """Nominal thread diameter from designation (e.g. M12 -> 12)."""
    key = normalize_designation(designation)
    return float(key[1:])


def across_corners_from_flats(across_flats: float) -> float:
    """Hex across-corners from across-flats (circumscribed cylinder)."""
    return across_flats * 2.0 / math.sqrt(3.0)


def chamfer_from_pitch(
    pitch: float, thickness: float, across_corners: float, bore: float
) -> float:
    """Face chamfer from thread pitch (ASME UNC / similar to ISO rule)."""
    c = 0.45 * pitch
    c = min(max(0.4, c), thickness * 0.35, 2.5)
    c = min(c, (across_corners - bore) * 0.35)
    return max(0.0, c)


def iso_face_chamfer_mm(
    thread_d: float, thickness: float, across_corners: float, bore: float
) -> float:
    """Typical ISO nut face chamfer (~0.45 x coarse pitch), clamped for ACIS."""
    td = int(round(thread_d))
    pitch = COARSE_PITCH_MM.get(td, thread_d * 0.15)
    c = 0.45 * pitch
    c = min(max(0.5, c), thickness * 0.35, 2.5)
    # Must fit inside CylinderChamfered (d_end = AC - 2c > bore).
    c = min(c, (across_corners - bore) * 0.35, (across_corners - thread_d) * 0.25)
    return max(0.0, c)


def _hex_side_cutters(s, across_flats: float, thickness: float) -> list:
    """Six boxes that trim a cylinder to a hex (width across flats = AF)."""
    apothem = across_flats / 2.0
    big = 4.0 * across_flats
    cutters = []
    for i in range(6):
        cutters.append(
            prim.Box(s, big, big, thickness + 2.0)
            .move(x=apothem + big / 2.0, z=-1.0)
            .rotateZ(i * 60.0)
        )
    return cutters


def _hex_nut(
    s,
    across_flats: float,
    across_corners: float,
    thickness: float,
    bore: float,
    *,
    chamfer: float = 0.0,
) -> prim.ShapeObject:
    """Hex nut: axis Z, bottom at z = 0.

    Chamfer approach: build a cylinder at AC with circular end chamfers
    (CylinderChamfered), then cut the six side flats — the bevel follows a
    circle on each face, matching typical ISO nut top/bottom detail.
    """
    c = chamfer
    if c > 0:
        body = prim.CylinderChamfered(
            s,
            diameter=across_corners,
            height=thickness,
            chamfer=c,
            chamfer_angle=30.0,
            double_chamfer=True,
        )
    else:
        body = prim.Cylinder(s, diameter=across_corners, height=thickness)

    body.subtract(_hex_side_cutters(s, across_flats, thickness))

    hole_chamfer = min(c * 0.5, 1.0, bore * 0.12) if c > 0 else 0.0
    if hole_chamfer > 0.2:
        hole = prim.CylinderChamfered(
            s,
            diameter=bore,
            height=thickness + 4.0,
            chamfer=hole_chamfer,
            chamfer_angle=45.0,
            double_chamfer=True,
        ).move(z=-2.0)
    else:
        hole = prim.Cylinder(s, diameter=bore, height=thickness + 2.0).move(z=-1.0)
    body.subtract(hole)

    return body


class Nut(prim.ShapeObject):
    """Hex nut along +Z (bottom at z = 0).

    ISO: ``Nut(s, Nut.Size.M12)`` from catalog table (AC / AF / thk).
    ASME heavy hex (stud bolts): ``Nut(s, across_flats=F, thickness=H,
    bolt_diameter=D, pitch=P)``.
    """

    class Size(str, Enum):
        M8 = "M8"
        M10 = "M10"
        M12 = "M12"
        M14 = "M14"
        M16 = "M16"
        M18 = "M18"
        M20 = "M20"
        M22 = "M22"
        M24 = "M24"
        M27 = "M27"
        M30 = "M30"
        M33 = "M33"
        M36 = "M36"
        M39 = "M39"
        M42 = "M42"
        M45 = "M45"
        M48 = "M48"
        M52 = "M52"
        M56 = "M56"
        M60 = "M60"
        M64 = "M64"
        M68 = "M68"

    def __init__(
        self,
        s,
        size=None,
        *,
        across_flats=None,
        across_corners=None,
        thickness=None,
        bolt_diameter=None,
        pitch=None,
        bore_clearance=BORE_CLEARANCE_MM,
        with_chamfer=True,
        chamfer=None,
    ):
        if across_flats is not None:
            if thickness is None or bolt_diameter is None:
                raise ValueError(
                    "thickness and bolt_diameter are required with across_flats."
                )
            self.designation = None
            self.af = float(across_flats)
            self.height = float(thickness)
            self.thread_d = float(bolt_diameter)
            self.ac = (
                float(across_corners)
                if across_corners is not None
                else across_corners_from_flats(self.af)
            )
            self.bore = self.thread_d + bore_clearance
            if chamfer is None:
                if pitch is not None:
                    c = chamfer_from_pitch(
                        pitch, self.height, self.ac, self.bore
                    )
                else:
                    c = iso_face_chamfer_mm(
                        self.thread_d, self.height, self.ac, self.bore
                    )
                self.chamfer = c if with_chamfer else 0.0
            else:
                self.chamfer = 0.0 if not with_chamfer else float(chamfer)
            label = f"F{self.af:.1f}"
        else:
            if size is None:
                size = "M12"
            key = normalize_designation(size)
            if key not in ISO_DIMENSIONS_MM:
                raise ValueError(
                    f"Unknown nut {key!r}. Valid: {sorted(ISO_DIMENSIONS_MM)}."
                )
            d = ISO_DIMENSIONS_MM[key]
            self.designation = key
            self.thread_d = thread_diameter_mm(key)
            self.ac = d["ac"]
            self.af = d["af"]
            self.height = d["thk"]
            self.bore = self.thread_d + bore_clearance
            if chamfer is None:
                c = iso_face_chamfer_mm(
                    self.thread_d, self.height, self.ac, self.bore
                )
                self.chamfer = c if with_chamfer else 0.0
            else:
                self.chamfer = 0.0 if not with_chamfer else float(chamfer)
            label = key

        if self.bore >= self.af:
            raise ValueError(f"{label}: bore must be smaller than across flats.")

        body = _hex_nut(
            s,
            self.af,
            self.ac,
            self.height,
            self.bore,
            chamfer=self.chamfer,
        )
        super().__init__(body.obj)
