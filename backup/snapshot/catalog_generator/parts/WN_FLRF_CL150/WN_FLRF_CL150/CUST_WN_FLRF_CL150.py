import math
from enum import IntEnum

import pipe_sizes
import primitives as prim


# Pipedata Pro: rf height = 1.5 mm (Class 150 RF)
RAISED_FACE_HEIGHT = 1.5


class WNFLRFCL150(prim.ShapeObject):
    """Weld Neck Flange, Raised Face (FLRF), ASME B16.5 Class 150.

    Dimensions from Pipedata Pro export (WN_FLRF_CL150). Column mapping:
        O   <- od
        tf  <- thickness
        X   <- hub-x
        A   <- hub-a  (pipe OD at weld end)
        Y   <- wn thk (overall length, excludes raised face)
        G   <- rf dia
        bcd <- pcd
        n   <- no of bolts
        hd  <- hole size for calc (mm)
        B   <- WN & long neck id (through bore / neck ID, mm)

    Default orientation: horizontal along X; raised face West (-X), hub East (+X).
    """

    class Size(IntEnum):
        DN15 = 15
        DN20 = 20
        DN25 = 25
        DN32 = 32
        DN40 = 40
        DN50 = 50
        DN65 = 65
        DN80 = 80
        DN90 = 90
        DN100 = 100
        DN125 = 125
        DN150 = 150
        DN200 = 200
        DN250 = 250
        DN300 = 300
        DN350 = 350
        DN400 = 400
        DN450 = 450

    # Pipedata Pro WN CL150 RF (mm), keyed by DN
    DIMENSIONS = {
        15:  {"O": 89,   "tf": 9.7,  "X": 30,   "A": 21.3,  "B": 15.76,  "Y": 46,  "bcd": 60.5,  "hd": 15.875, "n": 4,  "G": 34.9},
        20:  {"O": 99,   "tf": 11.2, "X": 38,   "A": 26.7,  "B": 20.96,  "Y": 51,  "bcd": 69.8,  "hd": 15.875, "n": 4,  "G": 42.9},
        25:  {"O": 108,  "tf": 12.7, "X": 49,   "A": 33.4,  "B": 26.64,  "Y": 54,  "bcd": 79.2,  "hd": 15.875, "n": 4,  "G": 50.8},
        32:  {"O": 117,  "tf": 14.2, "X": 59,   "A": 42.2,  "B": 35.08,  "Y": 56,  "bcd": 88.9,  "hd": 15.875, "n": 4,  "G": 63.5},
        40:  {"O": 127,  "tf": 15.9, "X": 65,   "A": 48.3,  "B": 40.94,  "Y": 60,  "bcd": 98.4,  "hd": 15.875, "n": 4,  "G": 73.0},
        50:  {"O": 152,  "tf": 17.5, "X": 78,   "A": 60.3,  "B": 52.48,  "Y": 62,  "bcd": 120.6, "hd": 19.05,  "n": 4,  "G": 92.1},
        65:  {"O": 178,  "tf": 20.6, "X": 90,   "A": 73.0,  "B": 62.68,  "Y": 68,  "bcd": 139.7, "hd": 19.05,  "n": 4,  "G": 104.8},
        80:  {"O": 190,  "tf": 22.4, "X": 108,  "A": 88.9,  "B": 77.92,  "Y": 68,  "bcd": 152.4, "hd": 19.05,  "n": 4,  "G": 127.0},
        90:  {"O": 216,  "tf": 22.4, "X": 122,  "A": 101.6, "B": 90.12,  "Y": 70,  "bcd": 177.8, "hd": 19.05,  "n": 8,  "G": 139.7},
        100: {"O": 229,  "tf": 22.4, "X": 135,  "A": 114.3, "B": 102.26, "Y": 75,  "bcd": 190.5, "hd": 19.05,  "n": 8,  "G": 157.2},
        125: {"O": 254,  "tf": 22.4, "X": 164,  "A": 141.3, "B": 128.2,  "Y": 87,  "bcd": 215.9, "hd": 22.225, "n": 8,  "G": 185.7},
        150: {"O": 279,  "tf": 23.9, "X": 192,  "A": 168.3, "B": 154.08, "Y": 87,  "bcd": 241.3, "hd": 22.225, "n": 8,  "G": 215.9},
        200: {"O": 343,  "tf": 26.9, "X": 246,  "A": 219.1, "B": 202.74, "Y": 100, "bcd": 298.4, "hd": 22.225, "n": 8,  "G": 269.9},
        250: {"O": 406,  "tf": 28.4, "X": 305,  "A": 273.0, "B": 254.46, "Y": 100, "bcd": 362.0, "hd": 25.4,   "n": 12, "G": 323.8},
        300: {"O": 483,  "tf": 30.2, "X": 365,  "A": 323.8, "B": 303.18, "Y": 113, "bcd": 431.8, "hd": 25.4,   "n": 12, "G": 381.0},
        350: {"O": 533,  "tf": 33.3, "X": 400,  "A": 355.6, "B": 333.34, "Y": 125, "bcd": 476.2, "hd": 28.575, "n": 12, "G": 412.8},
        400: {"O": 597,  "tf": 35.1, "X": 457,  "A": 406.4, "B": 381.0,  "Y": 125, "bcd": 539.8, "hd": 28.575, "n": 16, "G": 469.9},
        450: {"O": 635,  "tf": 38.1, "X": 505,  "A": 457.2, "B": 428.46, "Y": 138, "bcd": 577.8, "hd": 31.75,  "n": 16, "G": 533.4},
    }

    def __init__(self, s, size, *, add_ports=True):
        key = pipe_sizes.resolve_dn(size)
        if key not in self.DIMENSIONS:
            raise ValueError(f"No WN FLRF CL150 data for DN {key}.")

        d = self.DIMENSIONS[key]
        self.dn = key
        self.nps = pipe_sizes.dn_to_nps(key)
        self.O = d["O"]
        self.tf = d["tf"]
        self.X = d["X"]
        self.A = d["A"]
        self.B = d["B"]
        self.Y = d["Y"]
        self.bcd = d["bcd"]
        self.hd = d["hd"]
        self.n = d["n"]
        self.G = d["G"]

        rf_h = RAISED_FACE_HEIGHT
        hub_h = self.Y - self.tf
        if hub_h <= 0:
            raise ValueError("Y must be greater than tf to build the hub.")

        EPS = 0.5

        raised_face = prim.Cylinder(s, diameter=self.G, height=rf_h + EPS)
        disc = prim.Cylinder(s, diameter=self.O, height=self.tf).move(z=rf_h)
        hub = prim.Cone(
            s,
            bottom_diameter=self.X,
            height=hub_h + EPS,
            top_diameter=self.A,
        ).move(z=rf_h + self.tf - EPS)

        body = raised_face.combine([disc, hub])

        total_h = rf_h + self.Y
        bore = prim.Cylinder(s, diameter=self.B, height=total_h + 2).move(z=-1)

        holes = []
        offset_deg = 360.0 / self.n / 2.0
        for i in range(self.n):
            ang = math.radians(i * 360.0 / self.n + offset_deg)
            hx = (self.bcd / 2.0) * math.cos(ang)
            hy = (self.bcd / 2.0) * math.sin(ang)
            hole = prim.Cylinder(s, diameter=self.hd, height=self.tf + 2).move(
                x=hx, y=hy, z=rf_h - 1
            )
            holes.append(hole)

        body.subtract([bore] + holes)
        body.rotateY(90)

        self._rf_h = rf_h
        self._weld_port_x = rf_h + self.Y

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Two axial ports (X axis after rotateY(90)).

        Port 0 (West): FL / RF — gasket mating face.
        Port 1 (East): BV — weld neck butt-weld to pipe (catalog port 2).
        """
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self._weld_port_x, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
