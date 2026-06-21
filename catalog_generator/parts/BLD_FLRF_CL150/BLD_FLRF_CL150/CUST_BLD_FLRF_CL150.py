import math
from enum import IntEnum

import pipe_sizes
import primitives as prim


# Pipedata Pro: rf height = 1.5 mm (Class 150 RF)
RAISED_FACE_HEIGHT = 1.5


class BLDFLRFCL150(prim.ShapeObject):
    """Blind Flange, Raised Face (FLRF), ASME B16.5 Class 150.

    Dimensions from Pipedata Pro export (BLD_FLRF_CL150). Column mapping:
        O       <- od
        tf      <- thickness (flange body, excluding raised face)
        G       <- rf dia
        bcd     <- pcd
        n, hd   <- bolting

    Solid disc (no bore). Default orientation: horizontal along X;
    raised face West (-X), blind back East (+X).
    Future flat-face variant: BLD_FLFF_CL150.
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

    # Pipedata Pro Blind CL150 RF (mm)
    DIMENSIONS = {
        15:  {"O": 89,   "tf": 9.7,  "bcd": 60.5,  "hd": 15.875, "n": 4,  "G": 34.9},
        20:  {"O": 99,   "tf": 11.2, "bcd": 69.8,  "hd": 15.875, "n": 4,  "G": 42.9},
        25:  {"O": 108,  "tf": 12.7, "bcd": 79.2,  "hd": 15.875, "n": 4,  "G": 50.8},
        32:  {"O": 117,  "tf": 14.2, "bcd": 88.9,  "hd": 15.875, "n": 4,  "G": 63.5},
        40:  {"O": 127,  "tf": 15.9, "bcd": 98.4,  "hd": 15.875, "n": 4,  "G": 73.0},
        50:  {"O": 152,  "tf": 17.5, "bcd": 120.6, "hd": 19.05,  "n": 4,  "G": 92.1},
        65:  {"O": 178,  "tf": 20.6, "bcd": 139.7, "hd": 19.05,  "n": 4,  "G": 104.8},
        80:  {"O": 190,  "tf": 22.4, "bcd": 152.4, "hd": 19.05,  "n": 4,  "G": 127.0},
        90:  {"O": 216,  "tf": 22.4, "bcd": 177.8, "hd": 19.05,  "n": 8,  "G": 139.7},
        100: {"O": 229,  "tf": 22.4, "bcd": 190.5, "hd": 19.05,  "n": 8,  "G": 157.2},
        125: {"O": 254,  "tf": 22.4, "bcd": 215.9, "hd": 22.225, "n": 8,  "G": 185.7},
        150: {"O": 279,  "tf": 23.9, "bcd": 241.3, "hd": 22.225, "n": 8,  "G": 215.9},
        200: {"O": 343,  "tf": 26.9, "bcd": 298.4, "hd": 22.225, "n": 8,  "G": 269.9},
        250: {"O": 406,  "tf": 28.4, "bcd": 362.0, "hd": 25.4,   "n": 12, "G": 323.8},
        300: {"O": 483,  "tf": 30.2, "bcd": 431.8, "hd": 25.4,   "n": 12, "G": 381.0},
        350: {"O": 533,  "tf": 33.3, "bcd": 476.2, "hd": 28.575, "n": 12, "G": 412.8},
        400: {"O": 597,  "tf": 35.1, "bcd": 539.8, "hd": 28.575, "n": 16, "G": 469.9},
        450: {"O": 635,  "tf": 38.1, "bcd": 577.8, "hd": 31.75,  "n": 16, "G": 533.4},
    }

    def __init__(self, s, size, *, add_ports=True):
        key = pipe_sizes.resolve_dn(size)
        if key not in self.DIMENSIONS:
            raise ValueError(f"No Blind FLRF CL150 data for DN {key}.")

        d = self.DIMENSIONS[key]
        self.dn = key
        self.nps = pipe_sizes.dn_to_nps(key)
        self.O = d["O"]
        self.tf = d["tf"]
        self.bcd = d["bcd"]
        self.hd = d["hd"]
        self.n = d["n"]
        self.G = d["G"]

        rf_h = RAISED_FACE_HEIGHT
        EPS = 0.5

        raised_face = prim.Cylinder(s, diameter=self.G, height=rf_h + EPS)
        disc = prim.Cylinder(s, diameter=self.O, height=self.tf).move(z=rf_h)
        body = raised_face.combine([disc])

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

        body.subtract(holes)
        body.rotateY(90)

        self._rf_h = rf_h
        self._total_thk = rf_h + self.tf

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Single FL / RF connection at raised face (West, -X)."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        return self
