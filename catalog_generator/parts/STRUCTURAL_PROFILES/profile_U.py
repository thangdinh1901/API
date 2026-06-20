from enum import IntEnum
import STRUCTURAL_PROFILES._geometry as geom
from STRUCTURAL_PROFILES._sizes import resolve_size
from STRUCTURAL_PROFILES._positions import PositionU


class ProfileUPE(geom.prim.ShapeObject):
    class Size(IntEnum):
        UPE_80 = 80
        UPE_100 = 100
        UPE_120 = 120
        UPE_140 = 140
        UPE_160 = 160
        UPE_180 = 180
        UPE_200 = 200
        UPE_220 = 220
        UPE_240 = 240
        UPE_270 = 270
        UPE_300 = 300
        UPE_330 = 330
        UPE_360 = 360
        UPE_400 = 400

    DIMENSIONS = {
        80: {"H": 80, "B": 50, "t_w": 4.0, "t_f": 7.0, "y_s": 18.7, "r": 10},
        100: {"H": 100, "B": 55, "t_w": 4.5, "t_f": 7.5, "y_s": 19.5, "r": 10},
        120: {"H": 120, "B": 60, "t_w": 5.0, "t_f": 8.0, "y_s": 20.3, "r": 12},
        140: {"H": 140, "B": 65, "t_w": 5.0, "t_f": 9.0, "y_s": 22.2, "r": 12},
        160: {"H": 160, "B": 70, "t_w": 5.5, "t_f": 9.5, "y_s": 23.1, "r": 12},
        180: {"H": 180, "B": 75, "t_w": 5.5, "t_f": 10.5, "y_s": 25.1, "r": 12},
        200: {"H": 200, "B": 80, "t_w": 6.0, "t_f": 11.0, "y_s": 26.0, "r": 12},
        220: {"H": 220, "B": 85, "t_w": 6.5, "t_f": 12.0, "y_s": 27.4, "r": 13},
        240: {"H": 240, "B": 90, "t_w": 7.0, "t_f": 12.5, "y_s": 28.4, "r": 15},
        270: {"H": 270, "B": 95, "t_w": 7.5, "t_f": 13.5, "y_s": 29.3, "r": 15},
        300: {"H": 300, "B": 100, "t_w": 9.5, "t_f": 15.0, "y_s": 29.1, "r": 15},
        330: {"H": 330, "B": 105, "t_w": 11.0, "t_f": 16.0, "y_s": 29.3, "r": 18},
        360: {"H": 360, "B": 110, "t_w": 12.0, "t_f": 17.0, "y_s": 29.9, "r": 18},
        400: {"H": 400, "B": 115, "t_w": 13.5, "t_f": 18.0, "y_s": 30.0, "r": 18},
    }

    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionU = PositionU.CENTER,
    ):
        """
        Initialize the UPE profile with the specified size, height, and position.
        Parameters:
            - s: parameter of shape from Plant 3D
            - size: Size of the UPE profile (Size enum)
            - height: Height of the profile (float, default is 0.0)
            - position: Position of the profile (Position enum, default is CENTER)
        """
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.H = d["H"]
        self.B = d["B"]
        self.t_w = d["t_w"]
        self.t_f = d["t_f"]
        self.y_s = d["y_s"]
        self.r = d["r"]
        self.height = height

        o1 = geom.build_upe_shape(
            s, self.B, self.H, self.t_w, self.t_f, self.r, self.height
        )

        if position != PositionU.CENTER:
            translate_args = position.get_translation(self.B, self.H, self.y_s)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)


class ProfileUPN(geom.prim.ShapeObject):
    DIMENSIONS = {
        50: {
            "H": 50,
            "B": 38,
            "t_w": 5.0,
            "t_f": 7.0,
            "r1": 7.0,
            "r2": 3.50,
            "y_s": 13.70,
        },
        65: {
            "H": 65,
            "B": 42,
            "t_w": 5.5,
            "t_f": 7.5,
            "r1": 7.5,
            "r2": 4.00,
            "y_s": 14.20,
        },
        80: {
            "H": 80,
            "B": 45,
            "t_w": 6.0,
            "t_f": 8.0,
            "r1": 8.0,
            "r2": 4.00,
            "y_s": 14.50,
        },
        100: {
            "H": 100,
            "B": 50,
            "t_w": 6.0,
            "t_f": 8.5,
            "r1": 8.5,
            "r2": 4.50,
            "y_s": 15.50,
        },
        120: {
            "H": 120,
            "B": 55,
            "t_w": 7.0,
            "t_f": 9.0,
            "r1": 9.0,
            "r2": 4.50,
            "y_s": 16.00,
        },
        140: {
            "H": 140,
            "B": 60,
            "t_w": 7.0,
            "t_f": 10.0,
            "r1": 10.0,
            "r2": 5.00,
            "y_s": 17.50,
        },
        160: {
            "H": 160,
            "B": 65,
            "t_w": 7.5,
            "t_f": 10.5,
            "r1": 10.5,
            "r2": 5.50,
            "y_s": 18.40,
        },
        180: {
            "H": 180,
            "B": 70,
            "t_w": 8.0,
            "t_f": 11.0,
            "r1": 11.0,
            "r2": 5.50,
            "y_s": 19.20,
        },
        200: {
            "H": 200,
            "B": 75,
            "t_w": 8.5,
            "t_f": 11.5,
            "r1": 11.5,
            "r2": 5.75,
            "y_s": 20.10,
        },
        220: {
            "H": 220,
            "B": 80,
            "t_w": 9.0,
            "t_f": 12.5,
            "r1": 12.5,
            "r2": 6.25,
            "y_s": 21.40,
        },
        240: {
            "H": 240,
            "B": 85,
            "t_w": 9.5,
            "t_f": 13.0,
            "r1": 13.0,
            "r2": 6.50,
            "y_s": 22.30,
        },
        260: {
            "H": 260,
            "B": 90,
            "t_w": 10.0,
            "t_f": 14.0,
            "r1": 14.0,
            "r2": 7.00,
            "y_s": 23.60,
        },
        280: {
            "H": 280,
            "B": 95,
            "t_w": 10.0,
            "t_f": 15.0,
            "r1": 15.0,
            "r2": 7.50,
            "y_s": 25.30,
        },
        300: {
            "H": 300,
            "B": 100,
            "t_w": 10.0,
            "t_f": 16.0,
            "r1": 16.0,
            "r2": 8.00,
            "y_s": 27.00,
        },
        320: {
            "H": 320,
            "B": 100,
            "t_w": 14.0,
            "t_f": 17.5,
            "r1": 17.5,
            "r2": 8.75,
            "y_s": 26.00,
        },
        350: {
            "H": 350,
            "B": 100,
            "t_w": 14.0,
            "t_f": 16.0,
            "r1": 16.0,
            "r2": 8.00,
            "y_s": 24.00,
        },
        380: {
            "H": 380,
            "B": 102,
            "t_w": 13.5,
            "t_f": 16.0,
            "r1": 16.0,
            "r2": 8.00,
            "y_s": 23.80,
        },
        400: {
            "H": 400,
            "B": 110,
            "t_w": 14.0,
            "t_f": 18.0,
            "r1": 18.0,
            "r2": 9.00,
            "y_s": 26.50,
        },
    }

    class Size(IntEnum):
        UPN_50 = 50
        UPN_65 = 65
        UPN_80 = 80
        UPN_100 = 100
        UPN_120 = 120
        UPN_140 = 140
        UPN_160 = 160
        UPN_180 = 180
        UPN_200 = 200
        UPN_220 = 220
        UPN_240 = 240
        UPN_260 = 260
        UPN_280 = 280
        UPN_300 = 300
        UPN_320 = 320
        UPN_350 = 350
        UPN_380 = 380
        UPN_400 = 400

    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionU = PositionU.CENTER,
    ):
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.H = d["H"]
        self.B = d["B"]
        self.t_w = d["t_w"]
        self.t_f = d["t_f"]
        self.r1 = d["r1"]
        self.r2 = d["r2"]
        self.y_s = d["y_s"]
        self.height = height

        o1 = geom.build_upn_shape(
            s, self.B, self.H, self.t_w, self.t_f, self.r1, self.r2, self.height
        )

        if position != PositionU.CENTER:
            translate_args = position.get_translation(self.B, self.H, self.y_s)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)
