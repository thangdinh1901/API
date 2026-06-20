"""ASME B16.5 flange bolting data for STUD bolts (machine bolts are ignored).

Structure:
    BOLTING[pressure_class][nps] = {
        "bolt": <nominal bolt diameter, matches StudBolt.Size values>,
        "n":    <number of bolts in the flange>,
        "L":    {"RF": <mm>, "RTJ": <mm or None>},
    }

Selection axis: (NPS or DN, pressure class, face type).
"""

from enum import Enum

import pipe_sizes


class FaceType(str, Enum):
    RF = "RF"     # Raised Face
    FF = "FF"     # Flat Face
    LJ = "LJ"     # Lap Joint
    RTJ = "RTJ"   # Ring Type Joint


# Class 150 stud bolting: rf sb length & bolt count from Pipedata Pro (WN_FLRF_CL150);
# bolt diameter from bolt size unc column. RTJ lengths: ASME B16.5 where listed.
CLASS_150 = {
    "1/2":   {"bolt": "1/2",   "n": 4,  "L": {"RF": 55,  "RTJ": None}},
    "3/4":   {"bolt": "1/2",   "n": 4,  "L": {"RF": 65,  "RTJ": None}},
    "1":     {"bolt": "1/2",   "n": 4,  "L": {"RF": 65,  "RTJ": 75}},
    "1-1/4": {"bolt": "1/2",   "n": 4,  "L": {"RF": 70,  "RTJ": 85}},
    "1-1/2": {"bolt": "1/2",   "n": 4,  "L": {"RF": 70,  "RTJ": 85}},
    "2":     {"bolt": "5/8",   "n": 4,  "L": {"RF": 85,  "RTJ": 95}},
    "2-1/2": {"bolt": "5/8",   "n": 4,  "L": {"RF": 90,  "RTJ": 100}},
    "3":     {"bolt": "5/8",   "n": 4,  "L": {"RF": 90,  "RTJ": 100}},
    "3-1/2": {"bolt": "5/8",   "n": 8,  "L": {"RF": 90,  "RTJ": 100}},
    "4":     {"bolt": "5/8",   "n": 8,  "L": {"RF": 90,  "RTJ": 100}},
    "5":     {"bolt": "5/8",   "n": 8,  "L": {"RF": 95,  "RTJ": 110}},  # Pipedata: 5/8 stud, L=95
    "6":     {"bolt": "3/4",   "n": 8,  "L": {"RF": 100, "RTJ": 115}},
    "8":     {"bolt": "3/4",   "n": 8,  "L": {"RF": 110, "RTJ": 120}},
    "10":    {"bolt": "7/8",   "n": 12, "L": {"RF": 115, "RTJ": 125}},
    "12":    {"bolt": "7/8",   "n": 12, "L": {"RF": 120, "RTJ": 135}},
    "14":    {"bolt": "1",     "n": 12, "L": {"RF": 135, "RTJ": 145}},
    "16":    {"bolt": "1",     "n": 16, "L": {"RF": 135, "RTJ": 145}},
    "18":    {"bolt": "1-1/8", "n": 16, "L": {"RF": 145, "RTJ": 160}},
}


BOLTING = {
    150: CLASS_150,
}


def _length_key(face: FaceType) -> str:
    return "RTJ" if face == FaceType.RTJ else "RF"


def _resolve_nps(nps=None, dn=None) -> str:
    if dn is not None:
        return pipe_sizes.dn_to_nps(dn)
    if nps is None:
        raise ValueError("Provide nps= or dn=.")
    return str(nps)


def lookup(pressure_class, nps=None, dn=None, face: FaceType = FaceType.RF) -> dict:
    """Return {'bolt', 'n', 'L'} for flange NPS or DN / class / face."""
    key = _resolve_nps(nps=nps, dn=dn)
    pc = int(pressure_class)
    table = BOLTING.get(pc)
    if table is None:
        raise ValueError(
            f"No bolting data for Class {pc}. Available: {sorted(BOLTING.keys())}."
        )

    row = table.get(key)
    if row is None:
        raise ValueError(
            f"No NPS {key} in Class {pc}. Available: {list(table.keys())}."
        )

    if not isinstance(face, FaceType):
        face = FaceType(face)

    length = row["L"].get(_length_key(face))
    if length is None:
        raise ValueError(
            f"No {face.value} stud length for NPS {key} Class {pc}."
        )

    return {"bolt": row["bolt"], "n": row["n"], "L": length, "nps": key}
