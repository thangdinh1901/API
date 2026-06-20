import STRUCTURAL_PROFILES._geometry as geom
from enum import IntEnum
from STRUCTURAL_PROFILES._positions import PositionI
from STRUCTURAL_PROFILES._sizes import resolve_size


class ProfileIPN(geom.prim.ShapeObject):
    class Size(IntEnum):
        IPN_80 = 80
        IPN_100 = 100
        IPN_120 = 120
        IPN_140 = 140
        IPN_160 = 160
        IPN_180 = 180
        IPN_200 = 200
        IPN_220 = 220
        IPN_240 = 240
        IPN_260 = 260
        IPN_280 = 280
        IPN_300 = 300
        IPN_320 = 320
        IPN_340 = 340
        IPN_360 = 360
        IPN_380 = 380
        IPN_400 = 400
        IPN_450 = 450
        IPN_500 = 500
        IPN_550 = 550
        IPN_600 = 600

    DIMENSIONS = {
        80: {"B": 42, "H": 80, "t_w": 3.9, "t_f": 5.9, "R1": 3.9, "R2": 2.3},
        100: {"B": 50, "H": 100, "t_w": 4.5, "t_f": 6.8, "R1": 4.5, "R2": 2.7},
        120: {"B": 58, "H": 120, "t_w": 5.1, "t_f": 7.7, "R1": 5.1, "R2": 3.1},
        140: {"B": 66, "H": 140, "t_w": 5.7, "t_f": 8.6, "R1": 5.7, "R2": 3.4},
        160: {"B": 74, "H": 160, "t_w": 6.3, "t_f": 9.5, "R1": 6.3, "R2": 3.8},
        180: {"B": 82, "H": 180, "t_w": 6.9, "t_f": 10.4, "R1": 6.9, "R2": 4.1},
        200: {"B": 90, "H": 200, "t_w": 7.5, "t_f": 11.3, "R1": 7.5, "R2": 4.5},
        220: {"B": 98, "H": 220, "t_w": 8.1, "t_f": 12.2, "R1": 8.1, "R2": 4.9},
        240: {"B": 106, "H": 240, "t_w": 8.7, "t_f": 13.1, "R1": 8.7, "R2": 5.2},
        260: {"B": 113, "H": 260, "t_w": 9.4, "t_f": 14.1, "R1": 9.4, "R2": 5.6},
        280: {"B": 119, "H": 280, "t_w": 10.1, "t_f": 15.2, "R1": 10.1, "R2": 6.1},
        300: {"B": 125, "H": 300, "t_w": 10.8, "t_f": 16.2, "R1": 10.8, "R2": 6.5},
        320: {"B": 131, "H": 320, "t_w": 11.5, "t_f": 17.3, "R1": 11.5, "R2": 6.9},
        340: {"B": 137, "H": 340, "t_w": 12.2, "t_f": 18.3, "R1": 12.2, "R2": 7.3},
        360: {"B": 143, "H": 360, "t_w": 13.0, "t_f": 19.5, "R1": 13.0, "R2": 7.8},
        380: {"B": 149, "H": 380, "t_w": 13.7, "t_f": 20.5, "R1": 13.7, "R2": 8.2},
        400: {"B": 155, "H": 400, "t_w": 14.4, "t_f": 21.6, "R1": 14.4, "R2": 8.6},
        450: {"B": 170, "H": 450, "t_w": 16.2, "t_f": 24.3, "R1": 16.2, "R2": 9.7},
        500: {"B": 185, "H": 500, "t_w": 18.0, "t_f": 27.0, "R1": 18.0, "R2": 10.8},
        550: {"B": 200, "H": 550, "t_w": 19.0, "t_f": 30.0, "R1": 19.0, "R2": 11.9},
        600: {"B": 215, "H": 600, "t_w": 21.6, "t_f": 32.4, "R1": 21.6, "R2": 13.0},
    }
    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionI = PositionI.CENTER,
    ):
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.H = d["H"]
        self.B = d["B"]
        self.t_w = d["t_w"]
        self.t_f = d["t_f"]
        self.r1 = d["R1"]
        self.r2 = d["R2"]
        self.height = height

        o1 = geom.build_ipn_shape(
            s, self.B, self.H, self.t_w, self.t_f, self.r1, self.r2, self.height
        )

        if position != PositionI.CENTER:
            translate_args = position.get_translation(self.B, self.H)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)

class ProfileIPE(geom.prim.ShapeObject):
    class Size(IntEnum):
        IPE_80 = 80
        IPE_100 = 100
        IPE_120 = 120
        IPE_140 = 140
        IPE_160 = 160
        IPE_180 = 180
        IPE_200 = 200
        IPE_220 = 220
        IPE_240 = 240
        IPE_270 = 270
        IPE_300 = 300
        IPE_330 = 330
        IPE_360 = 360
        IPE_400 = 400
        IPE_450 = 450
        IPE_500 = 500
        IPE_550 = 550
        IPE_600 = 600

    DIMENSIONS = {
        80: {"b": 46, "h": 80, "t1": 3.8, "t2": 5.2, "R1": 5.0},
        100: {"b": 55, "h": 100, "t1": 4.1, "t2": 5.7, "R1": 7.0},
        120: {"b": 64, "h": 120, "t1": 4.4, "t2": 6.3, "R1": 7.0},
        140: {"b": 73, "h": 140, "t1": 4.7, "t2": 6.9, "R1": 7.0},
        160: {"b": 82, "h": 160, "t1": 5.0, "t2": 7.4, "R1": 9.0},
        180: {"b": 91, "h": 180, "t1": 5.3, "t2": 8.0, "R1": 9.0},
        200: {"b": 100, "h": 200, "t1": 5.6, "t2": 8.5, "R1": 12.0},
        220: {"b": 110, "h": 220, "t1": 5.9, "t2": 9.2, "R1": 12.0},
        240: {"b": 120, "h": 240, "t1": 6.2, "t2": 9.8, "R1": 15.0},
        270: {"b": 135, "h": 270, "t1": 6.6, "t2": 10.2, "R1": 15.0},
        300: {"b": 150, "h": 300, "t1": 7.1, "t2": 10.7, "R1": 15.0},
        330: {"b": 160, "h": 330, "t1": 7.5, "t2": 11.5, "R1": 18.0},
        360: {"b": 170, "h": 360, "t1": 8.0, "t2": 12.7, "R1": 18.0},
        400: {"b": 180, "h": 400, "t1": 8.6, "t2": 13.5, "R1": 21.0},
        450: {"b": 190, "h": 450, "t1": 9.4, "t2": 14.6, "R1": 21.0},
        500: {"b": 200, "h": 500, "t1": 10.2, "t2": 16.0, "R1": 21.0},
        550: {"b": 210, "h": 550, "t1": 11.1, "t2": 17.2, "R1": 24.0},
        600: {"b": 220, "h": 600, "t1": 12.0, "t2": 19.0, "R1": 24.0},
    }

    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionI = PositionI.CENTER,
    ):
        """
        Initialize the IPE profile with the specified size, height, and position.
        Parameters:
            - s: parameter of shape from Plant 3D
            - size: Size of the IPE profile (Size enum)
            - height: Height of the profile (float, default is 0.0)
            - position: Position of the profile (Position enum, default is CENTER)
        """
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.h = d["h"]
        self.b = d["b"]
        self.t1 = d["t1"]
        self.t2 = d["t2"]
        self.r1 = d["R1"]
        self.height = height

        o1 = geom.build_ipe_shape(
            s, self.b, self.h, self.t1, self.t2, self.r1, self.height
        )

        if position != PositionI.CENTER:
            translate_args = position.get_translation(self.b, self.h)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)

class ProfileHEA(geom.prim.ShapeObject):
    class Size(IntEnum):
        HEA_100 = 100
        HEA_120 = 120
        HEA_140 = 140
        HEA_160 = 160
        HEA_180 = 180
        HEA_200 = 200
        HEA_220 = 220
        HEA_240 = 240
        HEA_260 = 260
        HEA_280 = 280
        HEA_300 = 300
        HEA_320 = 320
        HEA_340 = 340
        HEA_360 = 360
        HEA_400 = 400
        HEA_450 = 450
        HEA_500 = 500
        HEA_550 = 550
        HEA_600 = 600
        HEA_650 = 650
        HEA_700 = 700
        HEA_800 = 800
        HEA_900 = 900
        HEA_1000 = 1000

    DIMENSIONS = {
        100: {"b": 100, "h": 96, "t1": 5, "t2": 8, "R1": 12},
        120: {"b": 120, "h": 114, "t1": 5, "t2": 8, "R1": 12},
        140: {"b": 140, "h": 133, "t1": 5.5, "t2": 8.5, "R1": 12},
        160: {"b": 160, "h": 152, "t1": 6, "t2": 9, "R1": 15},
        180: {"b": 180, "h": 171, "t1": 6, "t2": 9.5, "R1": 15},
        200: {"b": 200, "h": 190, "t1": 6.5, "t2": 10, "R1": 18},
        220: {"b": 220, "h": 210, "t1": 7, "t2": 11, "R1": 18},
        240: {"b": 240, "h": 230, "t1": 7.5, "t2": 12, "R1": 21},
        260: {"b": 260, "h": 250, "t1": 7.5, "t2": 12.5, "R1": 24},
        280: {"b": 280, "h": 270, "t1": 8, "t2": 13, "R1": 24},
        300: {"b": 300, "h": 290, "t1": 8.5, "t2": 14, "R1": 27},
        320: {"b": 300, "h": 310, "t1": 9, "t2": 15.5, "R1": 27},
        340: {"b": 300, "h": 330, "t1": 9.5, "t2": 16.5, "R1": 27},
        360: {"b": 300, "h": 350, "t1": 10, "t2": 17.5, "R1": 27},
        400: {"b": 300, "h": 390, "t1": 11, "t2": 19, "R1": 27},
        450: {"b": 300, "h": 440, "t1": 11.5, "t2": 21, "R1": 27},
        500: {"b": 300, "h": 490, "t1": 12, "t2": 23, "R1": 27},
        550: {"b": 300, "h": 540, "t1": 12.5, "t2": 24, "R1": 27},
        600: {"b": 300, "h": 590, "t1": 13, "t2": 25, "R1": 27},
        650: {"b": 300, "h": 640, "t1": 13.5, "t2": 26, "R1": 27},
        700: {"b": 300, "h": 690, "t1": 14.5, "t2": 27, "R1": 27},
        800: {"b": 300, "h": 790, "t1": 15, "t2": 28, "R1": 30},
        900: {"b": 300, "h": 890, "t1": 16, "t2": 30, "R1": 30},
        1000: {"b": 300, "h": 990, "t1": 16.5, "t2": 31, "R1": 30},
    }

    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionI = PositionI.CENTER,
    ):
        """
        Initialize the HEA profile with the specified size, height, and position.
        Parameters:
            - s: parameter of shape from Plant 3D
            - size: Size of the HEA profile (Size enum)
            - height: Height of the profile (float, default is 0.0)
            - position: Position of the profile (Position enum, default is CENTER)
        """
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.h = d["h"]
        self.b = d["b"]
        self.t1 = d["t1"]
        self.t2 = d["t2"]
        self.r1 = d["R1"]
        self.height = height

        o1 = geom.build_ipe_shape(
            s, self.b, self.h, self.t1, self.t2, self.r1, self.height
        )

        if position != PositionI.CENTER:
            translate_args = position.get_translation(self.b, self.h)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)

class ProfileHEB(geom.prim.ShapeObject):
    class Size(IntEnum):
        HEB_100 = 100
        HEB_120 = 120
        HEB_140 = 140
        HEB_160 = 160
        HEB_180 = 180
        HEB_200 = 200
        HEB_220 = 220
        HEB_240 = 240
        HEB_260 = 260
        HEB_280 = 280
        HEB_300 = 300
        HEB_320 = 320
        HEB_340 = 340
        HEB_360 = 360
        HEB_400 = 400
        HEB_450 = 450
        HEB_500 = 500
        HEB_550 = 550
        HEB_600 = 600
        HEB_650 = 650
        HEB_700 = 700
        HEB_800 = 800
        HEB_900 = 900
        HEB_1000 = 1000

    DIMENSIONS = {
        100: {"b": 100, "h": 100, "t1": 6, "t2": 10, "R1": 12},
        120: {"b": 120, "h": 120, "t1": 6.5, "t2": 11, "R1": 12},
        140: {"b": 140, "h": 140, "t1": 7, "t2": 12, "R1": 12},
        160: {"b": 160, "h": 160, "t1": 8, "t2": 13, "R1": 15},
        180: {"b": 180, "h": 180, "t1": 8.5, "t2": 14, "R1": 15},
        200: {"b": 200, "h": 200, "t1": 9, "t2": 15, "R1": 18},
        220: {"b": 220, "h": 220, "t1": 9.5, "t2": 16, "R1": 18},
        240: {"b": 240, "h": 240, "t1": 10, "t2": 17, "R1": 21},
        260: {"b": 260, "h": 260, "t1": 10, "t2": 17.5, "R1": 24},
        280: {"b": 280, "h": 280, "t1": 10.5, "t2": 18, "R1": 24},
        300: {"b": 300, "h": 300, "t1": 11, "t2": 19, "R1": 27},
        320: {"b": 300, "h": 320, "t1": 11.5, "t2": 20.5, "R1": 27},
        340: {"b": 300, "h": 340, "t1": 12, "t2": 21.5, "R1": 27},
        360: {"b": 300, "h": 360, "t1": 12.5, "t2": 22.5, "R1": 27},
        400: {"b": 300, "h": 400, "t1": 13.5, "t2": 24, "R1": 27},
        450: {"b": 300, "h": 450, "t1": 14, "t2": 26, "R1": 27},
        500: {"b": 300, "h": 500, "t1": 14.5, "t2": 28, "R1": 27},
        550: {"b": 300, "h": 550, "t1": 15, "t2": 29, "R1": 27},
        600: {"b": 300, "h": 600, "t1": 15.5, "t2": 30, "R1": 27},
        650: {"b": 300, "h": 650, "t1": 16, "t2": 31, "R1": 27},
        700: {"b": 300, "h": 700, "t1": 17, "t2": 32, "R1": 27},
        800: {"b": 300, "h": 800, "t1": 17.5, "t2": 33, "R1": 30},
        900: {"b": 300, "h": 900, "t1": 18.5, "t2": 35, "R1": 30},
        1000: {"b": 300, "h": 1000, "t1": 19, "t2": 36, "R1": 30},
    }

    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionI = PositionI.CENTER,
    ):
        """
        Initialize the HEB profile with the specified size, height, and position.
        Parameters:
            - s: parameter of shape from Plant 3D
            - size: Size of the HEB profile (Size enum)
            - height: Height of the profile (float, default is 0.0)
            - position: Position of the profile (Position enum, default is CENTER)
        """
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.h = d["h"]
        self.b = d["b"]
        self.t1 = d["t1"]
        self.t2 = d["t2"]
        self.r1 = d["R1"]
        self.height = height

        o1 = geom.build_ipe_shape(
            s, self.b, self.h, self.t1, self.t2, self.r1, self.height
        )

        if position != PositionI.CENTER:
            translate_args = position.get_translation(self.b, self.h)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)

class ProfileHEM(geom.prim.ShapeObject):
    class Size(IntEnum):
        HEM_100 = 100
        HEM_120 = 120
        HEM_140 = 140
        HEM_160 = 160
        HEM_180 = 180
        HEM_200 = 200
        HEM_220 = 220
        HEM_240 = 240
        HEM_260 = 260
        HEM_280 = 280
        HEM_300 = 300
        HEM_320 = 320
        HEM_340 = 340
        HEM_360 = 360
        HEM_400 = 400
        HEM_450 = 450
        HEM_500 = 500
        HEM_550 = 550
        HEM_600 = 600
        HEM_650 = 650
        HEM_700 = 700
        HEM_800 = 800
        HEM_900 = 900
        HEM_1000 = 1000


    DIMENSIONS = {
        100: {"b": 106, "h": 120, "t1": 12, "t2": 20, "R1": 12},
        120: {"b": 126, "h": 140, "t1": 12.5, "t2": 21, "R1": 12},
        140: {"b": 146, "h": 160, "t1": 13, "t2": 22, "R1": 12},
        160: {"b": 166, "h": 180, "t1": 14, "t2": 23, "R1": 15},
        180: {"b": 186, "h": 200, "t1": 14.5, "t2": 24, "R1": 15},
        200: {"b": 206, "h": 220, "t1": 15, "t2": 25, "R1": 18},
        220: {"b": 226, "h": 240, "t1": 15.5, "t2": 26, "R1": 18},
        240: {"b": 248, "h": 270, "t1": 18, "t2": 32, "R1": 21},
        260: {"b": 268, "h": 290, "t1": 18, "t2": 32.5, "R1": 24},
        280: {"b": 288, "h": 310, "t1": 18.5, "t2": 33, "R1": 24},
        300: {"b": 310, "h": 340, "t1": 21, "t2": 39, "R1": 27},
        320: {"b": 309, "h": 359, "t1": 21, "t2": 40, "R1": 27},
        340: {"b": 309, "h": 377, "t1": 21, "t2": 40, "R1": 27},
        360: {"b": 308, "h": 395, "t1": 21, "t2": 40, "R1": 27},
        400: {"b": 307, "h": 432, "t1": 21, "t2": 40, "R1": 27},
        450: {"b": 307, "h": 478, "t1": 21, "t2": 40, "R1": 27},
        500: {"b": 306, "h": 524, "t1": 21, "t2": 40, "R1": 27},
        550: {"b": 306, "h": 572, "t1": 21, "t2": 40, "R1": 27},
        600: {"b": 305, "h": 620, "t1": 21, "t2": 40, "R1": 27},
        650: {"b": 305, "h": 668, "t1": 21, "t2": 40, "R1": 27},
        700: {"b": 304, "h": 716, "t1": 21, "t2": 40, "R1": 27},
        800: {"b": 303, "h": 814, "t1": 21, "t2": 40, "R1": 30},
        900: {"b": 302, "h": 910, "t1": 21, "t2": 40, "R1": 30},
        1000: {"b": 302, "h": 1008, "t1": 21, "t2": 40, "R1": 30},
    }

    def __init__(
        self,
        s,
        size: Size | int,
        height: float = 0.0,
        position: PositionI = PositionI.CENTER,
    ):
        """
        Initialize the HEB profile with the specified size, height, and position.
        Parameters:
            - s: parameter of shape from Plant 3D
            - size: Size of the HEB profile (Size enum)
            - height: Height of the profile (float, default is 0.0)
            - position: Position of the profile (Position enum, default is CENTER)
        """
        size_enum = resolve_size(size, self.Size, self.DIMENSIONS)
        d = self.DIMENSIONS[size_enum.value]
        self.h = d["h"]
        self.b = d["b"]
        self.t1 = d["t1"]
        self.t2 = d["t2"]
        self.r1 = d["R1"]
        self.height = height

        o1 = geom.build_ipe_shape(
            s, self.b, self.h, self.t1, self.t2, self.r1, self.height
        )

        if position != PositionI.CENTER:
            translate_args = position.get_translation(self.b, self.h)
            if translate_args:
                o1.move(**translate_args)

        super().__init__(o1.obj)


        


