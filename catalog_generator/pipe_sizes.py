"""DN (mm) to NPS and piping/fitting dimensions for catalog parts.

Standard library set: BW_SCH40 (see standard_sets.json) — butt-weld fittings tagged
pipeSchedule 40. Envelope dimensions are per NPS (B16.9); same OD at bevel for Sch 40/80.

Standards (factory butt-weld fittings, mm):
    OD pipe      — ASME B36.10M (Schedule 40 outside diameter)
    LR 90°/45°   — ASME B16.9 center-to-end on long-radius elbows
    SR 90°       — ASME B16.9 center-to-end on short-radius elbows (NPS 1+)
    Equal tee    — ASME B16.9 straight tee C / M
    Reducer H    — ASME B16.9 concentric/eccentric reducer end-to-end
    Reducing tee — ASME B16.9 reducing outlet tee C (run) / M (branch)

Reference tables: pipingpipeline.com (B16.9), wermac.org, haihaogroup.com (B16.9-2018).
"""

STANDARD_SET_BW_SCH40 = "BW_SCH40"

DN_TO_NPS = {
    15: "1/2",
    20: "3/4",
    25: "1",
    32: "1-1/4",
    40: "1-1/2",
    50: "2",
    65: "2-1/2",
    80: "3",
    90: "3-1/2",
    100: "4",
    125: "5",
    150: "6",
    200: "8",
    250: "10",
    300: "12",
    350: "14",
    400: "16",
    450: "18",
}

NPS_TO_DN = {v: k for k, v in DN_TO_NPS.items()}

VALID_DN = tuple(DN_TO_NPS.keys())

# ASME B16.11 forged fittings — extra NPS for socket/threaded (1/8"–3/8").
SW_EXTRA_DN_TO_NPS = {
    6: "1/8",
    8: "1/4",
    10: "3/8",
}

SW_DN_TO_NPS = {**SW_EXTRA_DN_TO_NPS, **DN_TO_NPS}

# B16.11 Table I-1 — Class 3000 SW: A = center-to-bottom of socket (mm).
# Source: pipingpipeline.com ASME B16.11 SW tee/elbow tables.
SW_CL3000_CENTER_TO_SOCKET_MM = {
    6: 11.0,
    8: 11.0,
    10: 13.5,
    15: 15.5,
    20: 19.0,
    25: 22.5,
    32: 27.0,
    40: 32.0,
    50: 38.0,
    65: 41.0,
    80: 57.0,
    100: 66.5,
}

# B16.11 Class 3000 SW 45° elbow — center-to-bottom of socket B (mm).
SW_CL3000_ELBOW_45_CENTER_TO_SOCKET_MM = {
    6: 8.0,
    8: 8.0,
    10: 8.0,
    15: 11.0,
    20: 13.0,
    25: 14.0,
    32: 17.5,
    40: 20.5,
    50: 25.5,
    65: 28.5,
    80: 32.0,
    100: 41.0,
}

VALID_SW_CL3000_DN = tuple(SW_CL3000_CENTER_TO_SOCKET_MM.keys())

# B16.11 Class 3000 SW — B = socket bore diameter (mm), average of min/max.
# Source: pipingpipeline.com ASME B16.11 SW coupling tables.
SW_CL3000_SOCKET_BORE_MM = {
    6: 11.0,
    8: 14.4,
    10: 17.8,
    15: 22.0,
    20: 27.4,
    25: 34.1,
    32: 42.9,
    40: 49.0,
    50: 61.45,
    65: 74.15,
    80: 90.05,
    100: 115.45,
}

# B16.11 Class 3000 SW — J = minimum socket depth (mm).
SW_CL3000_SOCKET_DEPTH_MM = {
    6: 9.5,
    8: 9.5,
    10: 9.5,
    15: 9.5,
    20: 12.5,
    25: 12.5,
    32: 12.5,
    40: 12.5,
    50: 16.0,
    65: 16.0,
    80: 16.0,
    100: 19.0,
}

# B16.11 Class 3000 SW — C = socket wall thickness, average (mm).
SW_CL3000_SOCKET_WALL_MM = {
    6: 3.18,
    8: 3.78,
    10: 4.01,
    15: 4.67,
    20: 4.90,
    25: 5.69,
    32: 5.30,
    40: 5.60,
    50: 6.04,
    65: 7.67,
    80: 8.30,
    100: 9.35,
}

# B16.11 Class 3000 SW — D = minimum bore diameter of fitting (mm).
SW_CL3000_BORE_MM = {
    6: 6.1,
    8: 8.5,
    10: 11.8,
    15: 15.0,
    20: 20.2,
    25: 25.9,
    32: 34.3,
    40: 40.1,
    50: 51.7,
    65: 61.2,
    80: 76.4,
    100: 100.7,
}

STANDARD_SET_SW_CL3000 = "SW_CL3000"

# B16.11 §6.5 reducing SW tee: A from largest end (run DN).
_SW_TEE_REDUCE_ROWS = (
    (20, 15),
    (25, 20), (25, 15),
    (32, 25), (32, 20),
    (40, 32), (40, 25),
    (50, 40), (50, 32), (50, 25),
    (65, 50), (65, 40),
    (80, 65), (80, 50),
    (100, 80), (100, 65), (100, 50),
)

SW_TEE_REDUCE_PAIRS = frozenset(_SW_TEE_REDUCE_ROWS)

# Default piping spec: DN <= 40 -> Sch 80; DN >= 50 -> Sch 40.
SCH80_DN_MAX = 40
SCH40_DN_MIN = 50

# ASME B36.10M outside diameter (mm), Schedule 40.
OD_SCH40_MM = {
    15: 21.3,
    20: 26.7,
    25: 33.4,
    32: 42.2,
    40: 48.3,
    50: 60.3,
    65: 73.0,
    80: 88.9,
    90: 101.6,
    100: 114.3,
    125: 141.3,
    150: 168.3,
    200: 219.1,
    250: 273.0,
    300: 323.8,
    350: 355.6,
    400: 406.4,
    450: 457.2,
}

# Pipe OD (mm) for SW run modeling — B36.10M Sch-40 / small bore.
SW_PIPE_OD_MM = {
    6: 10.3,
    8: 13.7,
    10: 17.1,
    **OD_SCH40_MM,
}

# ASME B16.9 LR 90° butt weld elbow — center-to-face A (mm).
BW_ELBOW_LR90_CENTER_TO_FACE_MM = {
    15: 38,
    20: 38,
    25: 38,
    32: 48,
    40: 57,
    50: 76,
    65: 95,
    80: 114,
    90: 133,
    100: 152,
    125: 190,
    150: 229,
    200: 305,
    250: 381,
    300: 457,
    350: 533,
    400: 610,
    450: 686,
}

# ASME B36.10M nominal wall thickness t_n (mm), by DN.
SCH40_WALL_MM = {
    15: 2.77,
    20: 2.87,
    25: 3.38,
    32: 3.56,
    40: 3.68,
    50: 3.91,
    65: 4.78,
    80: 5.49,
    90: 5.74,
    100: 6.02,
    125: 6.55,
    150: 7.11,
    200: 8.18,
    250: 9.27,
    300: 10.31,
    350: 11.13,
    400: 12.70,
    450: 14.27,
}

SCH80_WALL_MM = {
    15: 3.73,
    20: 3.91,
    25: 4.55,
    32: 4.85,
    40: 5.08,
}

SO_FACE_RECESSION_CAP_MM = 6.0


def default_pipe_schedule(dn: int) -> str:
    """Schedule per project piping spec (DN mm)."""
    dn = int(dn)
    if dn <= SCH80_DN_MAX:
        return "Sch80"
    if dn >= SCH40_DN_MIN:
        return "Sch40"
    raise ValueError(
        f"DN {dn} is between Sch80 max ({SCH80_DN_MAX}) and Sch40 min "
        f"({SCH40_DN_MIN}); no default schedule."
    )


def nominal_wall_mm_default_spec(dn: int) -> float:
    """t_n (mm) for default spec: Sch 80 if DN <= 40, else Sch 40."""
    dn = int(dn)
    schedule = default_pipe_schedule(dn)
    table = SCH80_WALL_MM if schedule == "Sch80" else SCH40_WALL_MM
    try:
        return table[dn]
    except KeyError:
        raise ValueError(f"No {schedule} wall thickness for DN {dn}.") from None


def pipe_od_sch40_mm(dn: int) -> float:
    """Outside diameter (mm), ASME B36.10M Sch-40."""
    dn = int(dn)
    try:
        return OD_SCH40_MM[dn]
    except KeyError:
        raise ValueError(f"No Sch-40 OD for DN {dn}.") from None


def bw_elbow_lr90_center_to_face_mm(dn: int) -> float:
    """ASME B16.9 LR 90° elbow center-to-face / bend radius (mm)."""
    dn = int(dn)
    try:
        return BW_ELBOW_LR90_CENTER_TO_FACE_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.9 LR 90 elbow data for DN {dn}.") from None


# ASME B16.9 SR 90° butt weld elbow — center-to-face A (mm); 1D, NPS 1" and up only.
BW_ELBOW_SR90_CENTER_TO_FACE_MM = {
    25: 25,
    32: 32,
    40: 38,
    50: 51,
    65: 64,
    80: 76,
    90: 89,
    100: 102,
    125: 127,
    150: 152,
    200: 203,
    250: 254,
    300: 305,
    350: 356,
    400: 406,
    450: 457,
}

VALID_SR90_DN = tuple(BW_ELBOW_SR90_CENTER_TO_FACE_MM.keys())


def bw_elbow_sr90_center_to_face_mm(dn: int) -> float:
    """ASME B16.9 SR 90° elbow center-to-face / ARC3D bend radius (mm)."""
    dn = int(dn)
    try:
        return BW_ELBOW_SR90_CENTER_TO_FACE_MM[dn]
    except KeyError:
        raise ValueError(
            f"No B16.9 SR 90 elbow data for DN {dn} "
            f"(SR elbows start at NPS 1 / DN25)."
        ) from None


# ASME B16.9 LR 45° butt weld elbow — center-to-face B (mm).
BW_ELBOW_LR45_CENTER_TO_FACE_MM = {
    15: 16,
    20: 19,
    25: 22,
    32: 25,
    40: 29,
    50: 35,
    65: 44,
    80: 51,
    90: 57,
    100: 64,
    125: 79,
    150: 95,
    200: 127,
    250: 159,
    300: 190,
    350: 222,
    400: 254,
    450: 286,
}

# ASME B16.9 equal tee — center-to-end C and M (mm); equal tee C == M.
BW_TEE_EQUAL_CENTER_TO_END_MM = {
    15: 25,
    20: 29,
    25: 38,
    32: 48,
    40: 57,
    50: 64,
    65: 76,
    80: 86,
    90: 95,
    100: 105,
    125: 124,
    150: 143,
    200: 178,
    250: 216,
    300: 254,
    350: 279,
    400: 305,
    450: 343,
}

# ASME B16.9 concentric/eccentric reducer — end-to-end H (mm), keyed (dn_large, dn_small).
_BW_REDUCER_ROWS = (
    (20, 15, 38),
    (25, 20, 51), (25, 15, 51),
    (32, 25, 51), (32, 20, 51), (32, 15, 51),
    (40, 32, 64), (40, 25, 64), (40, 20, 64), (40, 15, 64),
    (50, 40, 76), (50, 32, 76), (50, 25, 76), (50, 20, 76),
    (65, 50, 89), (65, 40, 89), (65, 32, 89), (65, 25, 89),
    (80, 65, 89), (80, 50, 89), (80, 40, 89), (80, 32, 89),
    (90, 80, 102), (90, 65, 102), (90, 50, 102), (90, 40, 102), (90, 32, 102),
    (100, 90, 102), (100, 80, 102), (100, 65, 102), (100, 50, 102), (100, 40, 102),
    (125, 100, 127), (125, 90, 127), (125, 80, 127), (125, 65, 127), (125, 50, 127),
    (150, 125, 140), (150, 100, 140), (150, 90, 140), (150, 80, 140), (150, 65, 140),
    (200, 150, 152), (200, 125, 152), (200, 100, 152), (200, 90, 152),
    (250, 200, 178), (250, 150, 178), (250, 125, 178), (250, 100, 178),
    (300, 250, 203), (300, 200, 203), (300, 150, 203), (300, 125, 203),
    (350, 300, 330), (350, 250, 330), (350, 200, 330), (350, 150, 330),
    (400, 350, 356), (400, 300, 356), (400, 250, 356), (400, 200, 356),
    (450, 400, 381), (450, 350, 381), (450, 300, 381), (450, 250, 381),
)

BW_REDUCER_END_TO_END_MM = { (a, b): h for a, b, h in _BW_REDUCER_ROWS }


def bw_elbow_lr45_center_to_face_mm(dn: int) -> float:
    """ASME B16.9 LR 45° elbow center-to-face (mm)."""
    dn = int(dn)
    try:
        return BW_ELBOW_LR45_CENTER_TO_FACE_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.9 LR 45 elbow data for DN {dn}.") from None


def bw_tee_equal_center_to_end_mm(dn: int) -> float:
    """ASME B16.9 equal tee center-to-end C / M (mm)."""
    dn = int(dn)
    try:
        return BW_TEE_EQUAL_CENTER_TO_END_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.9 equal tee data for DN {dn}.") from None


def bw_reducer_end_to_end_mm(dn_large: int, dn_small: int) -> float:
    """ASME B16.9 reducer end-to-end length H (mm)."""
    dn_large = int(dn_large)
    dn_small = int(dn_small)
    if dn_small >= dn_large:
        raise ValueError(
            f"Reducer small DN ({dn_small}) must be less than large DN ({dn_large})."
        )
    try:
        return BW_REDUCER_END_TO_END_MM[(dn_large, dn_small)]
    except KeyError:
        raise ValueError(
            f"No B16.9 reducer data for DN {dn_large} x DN {dn_small}. "
            f"Valid small sizes for DN {dn_large}: "
            f"{sorted(b for a, b in BW_REDUCER_END_TO_END_MM if a == dn_large)}."
        ) from None


# ASME B16.9 reducing outlet tee — C (run) / M (branch), mm.
_BW_TEE_REDUCE_ROWS = (
    (20, 15, 29, 29),
    (25, 15, 38, 38), (25, 20, 38, 38),
    (32, 15, 48, 48), (32, 20, 48, 48), (32, 25, 48, 48),
    (40, 15, 57, 57), (40, 20, 57, 57), (40, 25, 57, 57), (40, 32, 57, 57),
    (50, 25, 64, 51), (50, 32, 64, 57), (50, 40, 64, 60),
    (65, 25, 76, 57), (65, 32, 76, 64), (65, 40, 76, 67), (65, 50, 76, 70),
    (80, 32, 86, 70), (80, 40, 86, 73), (80, 50, 86, 76), (80, 65, 86, 83),
    (90, 40, 95, 79), (90, 50, 95, 83), (90, 65, 95, 89), (90, 80, 95, 92),
    (100, 40, 105, 86), (100, 50, 105, 89), (100, 65, 105, 95),
    (100, 80, 105, 98), (100, 90, 105, 102),
    (125, 50, 124, 105), (125, 65, 124, 108), (125, 80, 124, 111),
    (125, 90, 124, 114), (125, 100, 124, 117),
    (150, 65, 143, 121), (150, 80, 143, 124), (150, 90, 143, 127),
    (150, 100, 143, 130), (150, 125, 143, 137),
    (200, 90, 178, 152), (200, 100, 178, 156), (200, 125, 178, 162),
    (200, 150, 178, 168),
    (250, 100, 216, 184), (250, 125, 216, 191), (250, 150, 216, 194),
    (250, 200, 216, 203),
    (300, 125, 254, 216), (300, 150, 254, 219), (300, 200, 254, 229),
    (300, 250, 254, 241),
    (350, 150, 279, 238), (350, 200, 279, 248), (350, 250, 279, 257),
    (350, 300, 279, 270),
    (400, 150, 305, 264), (400, 200, 305, 273), (400, 250, 305, 283),
    (400, 300, 305, 295), (400, 350, 305, 305),
    (450, 200, 343, 298), (450, 250, 343, 308), (450, 300, 343, 321),
    (450, 350, 343, 330), (450, 400, 343, 330),
)

BW_TEE_REDUCE_CENTER_TO_END_MM = {(a, b): (c, m) for a, b, c, m in _BW_TEE_REDUCE_ROWS}


def bw_tee_reducing_center_to_end_mm(dn_run: int, dn_branch: int) -> tuple[float, float]:
    """ASME B16.9 reducing tee (C run, M branch) in mm."""
    dn_run = int(dn_run)
    dn_branch = int(dn_branch)
    if dn_branch >= dn_run:
        raise ValueError(
            f"Tee branch DN ({dn_branch}) must be less than run DN ({dn_run})."
        )
    key = (dn_run, dn_branch)
    if key in BW_TEE_REDUCE_CENTER_TO_END_MM:
        return BW_TEE_REDUCE_CENTER_TO_END_MM[key]
    c = bw_tee_equal_center_to_end_mm(dn_run)
    m = bw_tee_equal_center_to_end_mm(dn_branch)
    return c, m


def default_reducer_small_dn(dn_large: int) -> int:
    """One standard step-down size for catalog defaults."""
    dn_large = int(dn_large)
    options = sorted(
        (b for a, b in BW_REDUCER_END_TO_END_MM if a == dn_large),
        reverse=True,
    )
    if not options:
        raise ValueError(f"No standard reducer small DN for DN {dn_large}.")
    return options[0]


def slip_on_face_recession_mm(nominal_wall_mm: float) -> float:
    """ASME B31.1 Fig. 127.4.4(B): pipe end setback from flange face."""
    return min(float(nominal_wall_mm), SO_FACE_RECESSION_CAP_MM)


def dn_to_nps(dn: int) -> str:
    try:
        return DN_TO_NPS[int(dn)]
    except KeyError:
        raise ValueError(
            f"Invalid DN {dn}. Valid: {list(DN_TO_NPS.keys())}."
        ) from None


def resolve_dn(size) -> int:
    """Accept int DN or an enum member with .value (DN mm)."""
    if hasattr(size, "value"):
        size = size.value
    dn = int(size)
    if dn not in DN_TO_NPS:
        raise ValueError(
            f"Invalid DN {dn}. Valid: {list(DN_TO_NPS.keys())}."
        )
    return dn


def resolve_sw_dn(size) -> int:
    """DN for ASME B16.11 Class 3000 SW fittings (NPS 1/8\"–4\")."""
    if hasattr(size, "value"):
        size = size.value
    dn = int(size)
    if dn not in SW_DN_TO_NPS:
        raise ValueError(
            f"Invalid SW DN {dn}. Valid: {list(SW_DN_TO_NPS.keys())}."
        )
    if dn not in SW_CL3000_CENTER_TO_SOCKET_MM:
        raise ValueError(
            f"DN {dn} is outside B16.11 Class 3000 SW range "
            f"(NPS 1/8\"–4\" / DN{min(VALID_SW_CL3000_DN)}–DN{max(VALID_SW_CL3000_DN)})."
        )
    return dn


def sw_dn_to_nps(dn: int) -> str:
    try:
        return SW_DN_TO_NPS[int(dn)]
    except KeyError:
        raise ValueError(f"Invalid SW DN {dn}.") from None


def sw_pipe_od_mm(dn: int) -> float:
    """Pipe OD (mm) for socket-weld run geometry — B36.10M."""
    dn = int(dn)
    try:
        return SW_PIPE_OD_MM[dn]
    except KeyError:
        raise ValueError(f"No SW pipe OD for DN {dn}.") from None


def sw_cl3000_socket_bore_mm(dn: int) -> float:
    """B16.11 Class 3000 SW — socket bore diameter B (mm)."""
    dn = resolve_sw_dn(dn)
    try:
        return SW_CL3000_SOCKET_BORE_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.11 CL3000 socket bore for DN {dn}.") from None


def sw_cl3000_socket_depth_mm(dn: int) -> float:
    """B16.11 Class 3000 SW — minimum socket depth J (mm)."""
    dn = resolve_sw_dn(dn)
    try:
        return SW_CL3000_SOCKET_DEPTH_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.11 CL3000 socket depth for DN {dn}.") from None


def sw_cl3000_socket_wall_mm(dn: int) -> float:
    """B16.11 Class 3000 SW — socket wall thickness C (mm)."""
    dn = resolve_sw_dn(dn)
    try:
        return SW_CL3000_SOCKET_WALL_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.11 CL3000 socket wall for DN {dn}.") from None


def sw_cl3000_forging_od_mm(dn: int) -> float:
    """Forging OD at socket end: B + 2C per B16.11 Class 3000 SW."""
    return sw_cl3000_socket_bore_mm(dn) + 2.0 * sw_cl3000_socket_wall_mm(dn)


def sw_cl3000_bore_mm(dn: int) -> float:
    """B16.11 Class 3000 SW — internal flow bore D (mm)."""
    dn = resolve_sw_dn(dn)
    try:
        return SW_CL3000_BORE_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.11 CL3000 bore for DN {dn}.") from None


def sw_cl3000_center_to_socket_mm(dn: int) -> float:
    """B16.11 Class 3000 SW — A: center to bottom of socket / inner shoulder (mm)."""
    dn = int(dn)
    try:
        return SW_CL3000_CENTER_TO_SOCKET_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.11 CL3000 SW dimension for DN {dn}.") from None


def sw_cl3000_center_to_outer_socket_mm(dn: int) -> float:
    """Center to outer face of socket forging = A + J (Plant 3D port plane)."""
    dn = resolve_sw_dn(dn)
    return sw_cl3000_center_to_socket_mm(dn) + sw_cl3000_socket_depth_mm(dn)


def sw_cl3000_elbow_45_center_to_outer_socket_mm(dn: int) -> float:
    """45° SW elbow: center to outer socket face = B + J."""
    dn = resolve_sw_dn(dn)
    return (
        sw_cl3000_elbow_45_center_to_socket_mm(dn) + sw_cl3000_socket_depth_mm(dn)
    )


def sw_cl3000_elbow_45_center_to_socket_mm(dn: int) -> float:
    """B16.11 Class 3000 SW 45° elbow — B: center to bottom of socket (mm)."""
    dn = int(dn)
    try:
        return SW_CL3000_ELBOW_45_CENTER_TO_SOCKET_MM[dn]
    except KeyError:
        raise ValueError(f"No B16.11 CL3000 SW 45 elbow for DN {dn}.") from None


def sw_tee_reducing_center_to_socket_mm(dn_run: int, dn_branch: int) -> tuple[float, float]:
    """B16.11 §6.5 reducing SW tee: C and M = A of largest (run) DN."""
    dn_run = resolve_sw_dn(dn_run)
    dn_branch = resolve_sw_dn(dn_branch)
    if dn_branch >= dn_run:
        raise ValueError(
            f"Tee branch DN ({dn_branch}) must be less than run DN ({dn_run})."
        )
    if (dn_run, dn_branch) not in SW_TEE_REDUCE_PAIRS:
        raise ValueError(
            f"No standard B16.11 SW reducing tee for DN {dn_run} x DN {dn_branch}."
        )
    a = sw_cl3000_center_to_socket_mm(dn_run)
    return a, a


def default_sw_tee_small_dn(dn_large: int) -> int:
    dn_large = int(dn_large)
    options = sorted(
        (b for a, b in _SW_TEE_REDUCE_ROWS if a == dn_large),
        reverse=True,
    )
    if not options:
        raise ValueError(f"No SW reducing tee branch DN for run DN {dn_large}.")
    return options[0]


# ASME B16.9 Type A lap-joint stub end — long pattern, Schedule STD/40 (mm).
# G = max lap OD, F = overall length (lap face to weld end), R = outside fillet,
# T = lap thickness (>= Sch-40 wall at bevel). Source: wermac.org B16.9 carbon steel.
STUBEND_LJ_A_LONG_MM = {
    15: {"G": 34.9, "F": 76.2, "R": 3.18, "T": 2.77},
    20: {"G": 42.9, "F": 76.2, "R": 3.18, "T": 2.87},
    25: {"G": 50.8, "F": 101.6, "R": 3.18, "T": 3.38},
    32: {"G": 63.5, "F": 101.6, "R": 4.76, "T": 3.56},
    40: {"G": 73.0, "F": 101.6, "R": 6.35, "T": 3.68},
    50: {"G": 92.1, "F": 152.4, "R": 7.94, "T": 3.91},
    65: {"G": 104.8, "F": 152.4, "R": 7.94, "T": 5.16},
    80: {"G": 127.0, "F": 152.4, "R": 9.53, "T": 5.49},
    90: {"G": 139.7, "F": 152.4, "R": 9.53, "T": 5.74},
    100: {"G": 157.2, "F": 152.4, "R": 11.11, "T": 6.02},
    125: {"G": 185.7, "F": 203.2, "R": 11.11, "T": 6.55},
    150: {"G": 215.9, "F": 203.2, "R": 12.70, "T": 7.11},
    200: {"G": 269.9, "F": 203.2, "R": 12.70, "T": 8.18},
    250: {"G": 323.9, "F": 254.0, "R": 12.70, "T": 9.27},
    300: {"G": 381.0, "F": 254.0, "R": 12.70, "T": 9.53},
    350: {"G": 412.8, "F": 304.8, "R": 12.70, "T": 9.53},
    400: {"G": 469.9, "F": 304.8, "R": 12.70, "T": 9.53},
    450: {"G": 533.4, "F": 304.8, "R": 12.70, "T": 9.53},
}


def stubend_lj_a_long_dims_mm(dn: int) -> dict[str, float]:
    """B16.9 Type A long-pattern lap stub end envelope (mm)."""
    dn = resolve_dn(dn)
    try:
        return dict(STUBEND_LJ_A_LONG_MM[dn])
    except KeyError:
        raise ValueError(f"No B16.9 Type A long stub end for DN {dn}.") from None


def stubend_lj_a_long_length_mm(dn: int) -> float:
    return stubend_lj_a_long_dims_mm(dn)["F"]


def pipe_id_sch40_mm(dn: int) -> float:
    """Inside diameter (mm) from B36.10M Sch-40 OD and wall."""
    dn = int(dn)
    return pipe_od_sch40_mm(dn) - 2.0 * SCH40_WALL_MM[dn]
