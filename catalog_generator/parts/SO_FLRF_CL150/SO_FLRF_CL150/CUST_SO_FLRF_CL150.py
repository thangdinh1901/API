import math
from enum import IntEnum

import pipe_sizes
import primitives as prim


# Pipedata Pro: rf height = 1.5 mm (Class 150 RF)
RAISED_FACE_HEIGHT = 1.5


def _pipe_face_recession_mm(dn: int) -> float:
    """Pipe end setback from flat flange face (ASME B31.1 Fig. 127.4.4(B))."""
    t_n = pipe_sizes.nominal_wall_mm_default_spec(dn)
    return pipe_sizes.slip_on_face_recession_mm(t_n)


class SOFLRFCL150(prim.ShapeObject):
    """Slip-On Flange, Raised Face (FLRF), ASME B16.5 Class 150.

    Dimensions from Pipedata Pro export (SO_FLRF_CL150). Column mapping:
        O       <- od
        tf      <- thickness (flange disc / ring, e.g. 22.4 for 4")
        thd_thk <- thd thk = total height through hub EXCLUDING raised face
                  (back of flange to top of hub, e.g. 32 for 4"); NOT hub length alone
        hub_len <- thd_thk - tf (short hub cylinder above the disc)
        hub_OD  <- hub-x
        B       <- SO Bore
        G       <- rf dia
        bcd     <- pcd
        n, hd   <- bolting

    Default orientation: horizontal along X; raised face West (-X), hub East (+X).
    Future flat-face variant: SO_FLFF_CL150.
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

    # Pipedata Pro SO CL150 RF (mm). "thd_thk" = length through hub (excl. RF), per drawing.
    DIMENSIONS = {
        15:  {"O": 89,   "tf": 9.7,  "hub_OD": 30,  "thd_thk": 14, "B": 22.2,  "bcd": 60.5,  "hd": 15.875, "n": 4,  "G": 34.9},
        20:  {"O": 99,   "tf": 11.2, "hub_OD": 38,  "thd_thk": 14, "B": 27.7,  "bcd": 69.8,  "hd": 15.875, "n": 4,  "G": 42.9},
        25:  {"O": 108,  "tf": 12.7, "hub_OD": 49,  "thd_thk": 16, "B": 34.5,  "bcd": 79.2,  "hd": 15.875, "n": 4,  "G": 50.8},
        32:  {"O": 117,  "tf": 14.2, "hub_OD": 59,  "thd_thk": 19, "B": 43.2,  "bcd": 88.9,  "hd": 15.875, "n": 4,  "G": 63.5},
        40:  {"O": 127,  "tf": 15.9, "hub_OD": 65,  "thd_thk": 21, "B": 49.5,  "bcd": 98.4,  "hd": 15.875, "n": 4,  "G": 73.0},
        50:  {"O": 152,  "tf": 17.5, "hub_OD": 78,  "thd_thk": 24, "B": 61.9,  "bcd": 120.6, "hd": 19.05,  "n": 4,  "G": 92.1},
        65:  {"O": 178,  "tf": 20.6, "hub_OD": 90,  "thd_thk": 27, "B": 74.6,  "bcd": 139.7, "hd": 19.05,  "n": 4,  "G": 104.8},
        80:  {"O": 190,  "tf": 22.4, "hub_OD": 108, "thd_thk": 29, "B": 90.7,  "bcd": 152.4, "hd": 19.05,  "n": 4,  "G": 127.0},
        90:  {"O": 216,  "tf": 22.4, "hub_OD": 122, "thd_thk": 30, "B": 103.4, "bcd": 177.8, "hd": 19.05,  "n": 8,  "G": 139.7},
        100: {"O": 229,  "tf": 22.4, "hub_OD": 135, "thd_thk": 32, "B": 116.8, "bcd": 190.5, "hd": 19.05,  "n": 8,  "G": 157.2},
        125: {"O": 254,  "tf": 22.4, "hub_OD": 164, "thd_thk": 35, "B": 143.8, "bcd": 215.9, "hd": 22.225, "n": 8,  "G": 185.7},
        150: {"O": 279,  "tf": 23.9, "hub_OD": 192, "thd_thk": 38, "B": 170.7, "bcd": 241.3, "hd": 22.225, "n": 8,  "G": 215.9},
        200: {"O": 343,  "tf": 26.9, "hub_OD": 246, "thd_thk": 43, "B": 221.5, "bcd": 298.4, "hd": 22.225, "n": 8,  "G": 269.9},
        250: {"O": 406,  "tf": 28.4, "hub_OD": 305, "thd_thk": 48, "B": 276.2, "bcd": 362.0, "hd": 25.4,   "n": 12, "G": 323.8},
        300: {"O": 483,  "tf": 30.2, "hub_OD": 365, "thd_thk": 54, "B": 327.0, "bcd": 431.8, "hd": 25.4,   "n": 12, "G": 381.0},
        350: {"O": 533,  "tf": 33.3, "hub_OD": 400, "thd_thk": 56, "B": 359.2, "bcd": 476.2, "hd": 28.575, "n": 12, "G": 412.8},
        400: {"O": 597,  "tf": 35.1, "hub_OD": 457, "thd_thk": 62, "B": 410.5, "bcd": 539.8, "hd": 28.575, "n": 16, "G": 469.9},
        450: {"O": 635,  "tf": 38.1, "hub_OD": 505, "thd_thk": 67, "B": 461.8, "bcd": 577.8, "hd": 31.75,  "n": 16, "G": 533.4},
    }

    def __init__(self, s, size, cel_mm=None, *, add_ports=True):
        key = pipe_sizes.resolve_dn(size)
        if key not in self.DIMENSIONS:
            raise ValueError(f"No SO FLRF CL150 data for DN {key}.")

        d = self.DIMENSIONS[key]
        self.dn = key
        self.nps = pipe_sizes.dn_to_nps(key)
        self.O = d["O"]
        self.tf = d["tf"]
        self.B = d["B"]
        self.thd_thk = d["thd_thk"]
        self.hub_OD = d["hub_OD"]
        self.bcd = d["bcd"]
        self.hd = d["hd"]
        self.n = d["n"]
        self.G = d["G"]
        self.pipe_schedule = pipe_sizes.default_pipe_schedule(key)
        self.t_n = pipe_sizes.nominal_wall_mm_default_spec(key)
        self.face_recession = _pipe_face_recession_mm(key)

        # Hub cylinder sits above the disc; thd_thk is total body height (excl. RF).
        self.hub_len = self.thd_thk - self.tf
        if self.hub_len <= 0:
            raise ValueError(
                f"DN {key}: thd_thk ({self.thd_thk}) must exceed tf ({self.tf})."
            )

        rf_h = RAISED_FACE_HEIGHT
        EPS = 0.5

        raised_face = prim.Cylinder(s, diameter=self.G, height=rf_h + EPS)
        disc = prim.Cylinder(s, diameter=self.O, height=self.tf).move(z=rf_h)
        hub = prim.Cylinder(s, diameter=self.hub_OD, height=self.hub_len + EPS).move(
            z=rf_h + self.tf - EPS
        )

        body = raised_face.combine([disc, hub])

        total_h = rf_h + self.thd_thk
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
        self._flat_face_x = rf_h
        self._hub_top_x = rf_h + self.thd_thk
        # B31.1 setback (for pipecut / iso; not the Plant SO connection port).
        self._pipe_end_x = self._flat_face_x + self.face_recession
        self.cel_engagement = (
            float(cel_mm)
            if cel_mm is not None
            else self._hub_top_x - self._pipe_end_x
        )
        # Plant SO connector expects the port on the flat disc face (like native
        # CS150 fittings), not recessed at the pipe plain end.
        self._pipe_port_x = self._flat_face_x

        super().__init__(body.obj)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Two axial ports (X axis after rotateY(90)).

        Port 0 (West): FL / RF — gasket mating face.
        Port 1 (East): SO on flat disc face (x = rf_h); catalog port 2, EndType SO.
        CEL dimension: pipe plain end (B31.1 setback) → hub top; matches size-table CEL.
        """
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(self._pipe_port_x, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        prim.set_dimension(
            s,
            "CEL",
            prim.Point3d(self._pipe_end_x, 0.0, 0.0),
            prim.Point3d(self._hub_top_x, 0.0, 0.0),
        )
        return self
