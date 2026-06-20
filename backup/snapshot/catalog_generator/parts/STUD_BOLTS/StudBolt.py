import math
from enum import Enum

import primitives as prim
from NUTS.Nut import Nut
from STUD_BOLTS import bolting_data


class StudBolt(prim.ShapeAssembly):
    """Stud bolt assembly: threaded rod (ASME A193 B7) with two heavy hex
    nuts (ASME A194 2H), built via ``Nuts.Nut`` (hex + face chamfer).

    Default orientation: horizontal along X (same connection axis as the WN
    flange in this catalog). Internally the rod is built along +Z (z = 0 ..
    length) with a heavy hex nut near each end and the rod protruding a few
    threads past each nut per common practice, then rotated rotateY(90) into
    the default horizontal pose. The thread profile itself is simplified to a
    plain cylinder with 45 deg chamfers on both ends (standard for plant
    models); the accurate data are the nominal diameter and the heavy hex
    nut dimensions per ASME B18.2.2.

    Dimensions (mm):
        D : nominal bolt diameter
        F : heavy hex nut width across flats
        H : heavy hex nut thickness
        P : thread pitch (UNC / 8UN)
    """

    class Size(str, Enum):
        D1_2 = "1/2"
        D5_8 = "5/8"
        D3_4 = "3/4"
        D7_8 = "7/8"
        D1 = "1"
        D1_1_8 = "1-1/8"
        D1_1_4 = "1-1/4"
        D1_3_8 = "1-3/8"
        D1_1_2 = "1-1/2"

    # D, heavy hex nut F (across flats) & H (thickness), and thread pitch P, in mm.
    DIMENSIONS = {
        "1/2":   {"D": 12.700, "F": 22.225, "H": 12.303, "P": 1.954},
        "5/8":   {"D": 15.875, "F": 26.988, "H": 15.478, "P": 2.309},
        "3/4":   {"D": 19.050, "F": 31.750, "H": 18.653, "P": 2.540},
        "7/8":   {"D": 22.225, "F": 36.513, "H": 21.828, "P": 2.822},
        "1":     {"D": 25.400, "F": 41.275, "H": 25.003, "P": 3.175},
        "1-1/8": {"D": 28.575, "F": 46.038, "H": 28.178, "P": 3.175},
        "1-1/4": {"D": 31.750, "F": 50.800, "H": 30.956, "P": 3.175},
        "1-3/8": {"D": 34.925, "F": 55.563, "H": 34.131, "P": 3.175},
        "1-1/2": {"D": 38.100, "F": 60.325, "H": 37.306, "P": 3.175},
    }

    # Nut bore clearance over the nominal diameter (avoids coincident faces).
    BORE_CLEARANCE = 0.5
    ROD_CHAMFER_ANGLE_DEG = 45.0

    @classmethod
    def _rod_chamfer_mm(cls, diameter: float, pitch: float) -> float:
        """End chamfer width on rod OD (~half a thread, capped by bolt size)."""
        c = min(0.5 * pitch, diameter * 0.12, 2.5)
        return max(0.4, c)

    @classmethod
    def _make_rod(cls, s, diameter: float, length: float, pitch: float):
        c = cls._rod_chamfer_mm(diameter, pitch)
        chamfer_axial = c / math.tan(math.radians(cls.ROD_CHAMFER_ANGLE_DEG))
        if length <= 2.0 * chamfer_axial + 1.0:
            return prim.Cylinder(s, diameter=diameter, height=length), c
        rod = prim.CylinderChamfered(
            s,
            diameter=diameter,
            height=length,
            chamfer=c,
            chamfer_angle=cls.ROD_CHAMFER_ANGLE_DEG,
            double_chamfer=True,
        )
        return rod, c

    @classmethod
    def _make_nut(cls, s, across_flats: float, thickness: float, bolt_d: float, pitch: float):
        return Nut(
            s,
            across_flats=across_flats,
            thickness=thickness,
            bolt_diameter=bolt_d,
            pitch=pitch,
            bore_clearance=cls.BORE_CLEARANCE,
        )

    @classmethod
    def from_flange(
        cls,
        s,
        nps=None,
        dn=None,
        pressure_class=150,
        face=bolting_data.FaceType.RF,
        protruding_threads: float = 3.0,
        register_ports=True,
    ):
        """Build the ASME B16.5-compliant stud for a given flange.

        Selection axis: (DN or NPS, pressure class, face type). Pass dn= (mm)
        for catalog sizes, or nps= ('4', '1-1/2', ...).

        Metadata on the returned object:
            .dn, .nps, .pressure_class, .face, .n_bolts
        """
        import pipe_sizes

        info = bolting_data.lookup(pressure_class, nps=nps, dn=dn, face=face)
        stud = cls(
            s,
            info["bolt"],
            length=info["L"],
            protruding_threads=protruding_threads,
            register_ports=register_ports,
        )
        stud.nps = info["nps"]
        stud.dn = int(dn) if dn is not None else pipe_sizes.NPS_TO_DN.get(info["nps"])
        stud.pressure_class = int(pressure_class)
        stud.face = (
            face
            if isinstance(face, bolting_data.FaceType)
            else bolting_data.FaceType(face)
        )
        stud.n_bolts = info["n"]
        return stud

    def __init__(
        self,
        s,
        size,
        length: float = 100.0,
        protruding_threads: float = 3.0,
        register_ports=True,
    ):
        key = size.value if isinstance(size, StudBolt.Size) else size
        if key not in self.DIMENSIONS:
            raise ValueError(
                f"Invalid size: {size}. Choose one of {list(self.DIMENSIONS.keys())}."
            )

        d = self.DIMENSIONS[key]
        self.D = d["D"]
        self.F = d["F"]
        self.H = d["H"]
        self.P = d["P"]
        self.length = length

        # Length of rod protruding past each nut (a few threads, per practice)
        self.protrusion = protruding_threads * self.P

        if length <= 2.0 * (self.H + self.protrusion):
            raise ValueError(
                f"length ({length}) must exceed 2*(nut + protrusion) "
                f"= {2.0 * (self.H + self.protrusion):.1f} mm."
            )

        rod, self.rod_chamfer = self._make_rod(s, self.D, length, self.P)

        nut_bottom = self._make_nut(s, self.F, self.H, self.D, self.P).move(
            z=self.protrusion
        )
        nut_top = self._make_nut(s, self.F, self.H, self.D, self.P).move(
            z=length - self.protrusion - self.H
        )

        # Nut bearing faces (where the nuts clamp the flange stack). These are
        # the natural connection references for a flanged joint.
        self.bearing_bottom = self.protrusion + self.H
        self.bearing_top = length - self.protrusion - self.H

        super().__init__(rod, nut_bottom, nut_top)

        # Default orientation: lay the stud horizontal along X (same axis as the
        # flange). rotateY(90) maps the build axis +Z -> +X.
        self.rotateY(90)
        if register_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Define two axial connection ports at the nut bearing faces (X axis),
        each pointing inward toward the clamped flange stack. The catalog
        FamilyTemplate must declare PortNum = 2 to match."""
        prim.set_port(
            s,
            prim.Point3d(self.bearing_bottom, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self.bearing_top, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        return self
