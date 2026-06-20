from enum import IntEnum
import math


class PositionI(IntEnum):
    """
    Structural IPE Profile Positions (ASCII Diagram):

        TOP_LEFT--------TOP-------TOP_RIGHT
                         |
                         |
        LEFT-----------CENTER-----RIGHT
                         |
                         |
        BOT_LEFT--------BOT-------BOT_RIGHT

    """

    CENTER = 0
    RIGHT = 1
    LEFT = 2
    TOP = 3
    TOP_RIGHT = 4
    TOP_LEFT = 5
    BOT = 6
    BOT_RIGHT = 7
    BOT_LEFT = 8

    def get_translation(self, B: float, H: float) -> dict[str, float]:
        pos_map: dict[PositionI, dict[str, float]] = {
            PositionI.CENTER: {},
            PositionI.RIGHT: {"x": -B / 2},
            PositionI.LEFT: {"x": B / 2},
            PositionI.TOP: {"y": -H / 2},
            PositionI.TOP_RIGHT: {"x": -B / 2, "y": -H / 2},
            PositionI.TOP_LEFT: {"x": B / 2, "y": -H / 2},
            PositionI.BOT: {"y": H / 2},
            PositionI.BOT_RIGHT: {"x": -B / 2, "y": H / 2},
            PositionI.BOT_LEFT: {"x": B / 2, "y": H / 2},
        }
        return pos_map.get(self, {})


class PositionU(IntEnum):
    """
    Structural UPE Profile Positions (ASCII Diagram):

        TOP_LEFT----TOP_CENTROID----TOP-------TOP_RIGHT
        |
        |
        LEFT--------CENTROID--------CENTER-----RIGHT
        |
        |
        BOT_LEFT----BOT_CENTROID-----BOT-------BOT_RIGHT

    """

    CENTER = 0
    RIGHT = 1
    LEFT = 2
    CENTROID = 3
    TOP = 4
    TOP_RIGHT = 5
    TOP_LEFT = 6
    TOP_CENTROID = 7
    BOT = 8
    BOT_RIGHT = 9
    BOT_LEFT = 10
    BOT_CENTROID = 11

    def get_translation(self, B: float, H: float, y_s: float) -> dict[str, float]:
        pos_map: dict[PositionU, dict[str, float]] = {
            PositionU.CENTER: {},
            PositionU.RIGHT: {"x": -B / 2},
            PositionU.LEFT: {"x": B / 2},
            PositionU.CENTROID: {"x": B / 2 - y_s},
            PositionU.TOP: {"y": -H / 2},
            PositionU.TOP_RIGHT: {"x": -B / 2, "y": -H / 2},
            PositionU.TOP_LEFT: {"x": B / 2, "y": -H / 2},
            PositionU.TOP_CENTROID: {"x": B / 2 - y_s, "y": -H / 2},
            PositionU.BOT: {"y": H / 2},
            PositionU.BOT_RIGHT: {"x": -B / 2, "y": H / 2},
            PositionU.BOT_LEFT: {"x": B / 2, "y": H / 2},
            PositionU.BOT_CENTROID: {"x": B / 2 - y_s, "y": H / 2},
        }
        return pos_map.get(self, {})


class PositionL(IntEnum):
    """
    Structural L Profile Positions (ASCII Diagram):

        TOP_LEFT        TOP_CENTROID        TOP                  TOP_RIGHT
        ||
        ||
        ||
        ||
        CENTER_LEFT     CENTER_CENTROID     CENTER               CENTER_RIGHT
        ||
        CENTROID_LEFT   CENTROID            CENTROID_CENTER      CENTROID_RIGHT
        ||
        ||
        BOT_LEFT========BOT_CENTROID========BOT==================BOT_RIGHT

    """

    CENTER = 0
    CENTER_RIGHT = 1
    CENTER_LEFT = 2
    CENTER_CENTROID = 3
    TOP = 4
    TOP_RIGHT = 5
    TOP_LEFT = 6
    TOP_CENTROID = 7
    BOT = 8
    BOT_RIGHT = 9
    BOT_LEFT = 10
    BOT_CENTROID = 11
    CENTROID_CENTER = 12
    CENTROID = 13
    CENTROID_RIGHT = 14
    CENTROID_LEFT = 15

    def get_translation(
        self, B: float, H: float, x_s: float, y_s: float
    ) -> dict[str, float]:
        pos_map: dict[PositionL, dict[str, float]] = {
            PositionL.CENTER: {},
            PositionL.CENTER_RIGHT: {"x": -B / 2},
            PositionL.CENTER_LEFT: {"x": B / 2},
            PositionL.CENTER_CENTROID: {"x": B / 2 - x_s},
            PositionL.TOP: {"y": -H / 2},
            PositionL.TOP_RIGHT: {"x": -B / 2, "y": -H / 2},
            PositionL.TOP_LEFT: {"x": B / 2, "y": -H / 2},
            PositionL.TOP_CENTROID: {"x": B / 2 - x_s, "y": -H / 2},
            PositionL.BOT: {"y": H / 2},
            PositionL.BOT_RIGHT: {"x": -B / 2, "y": H / 2},
            PositionL.BOT_LEFT: {"x": B / 2, "y": H / 2},
            PositionL.BOT_CENTROID: {"x": B / 2 - x_s, "y": H / 2},
            PositionL.CENTROID_CENTER: {"y": H / 2 - y_s},
            PositionL.CENTROID: {"x": B / 2 - x_s, "y": H / 2 - y_s},
            PositionL.CENTROID_RIGHT: {"x": -B / 2, "y": H / 2 - y_s},
            PositionL.CENTROID_LEFT: {"x": B / 2, "y": H / 2 - y_s},
        }
        return pos_map.get(self, {})

    def get_translation_custom(
        self, b: float, h: float, t: float, r1: float, r2: float
    ) -> dict[str, float]:
        CONST1 = (5 / 6 - math.pi / 4) / (1 - math.pi / 4)  # (1/2-Pi)/(1-Pi)
        CONST2 = 1 - math.pi / 4  # (1-Pi)

        # horizontal flange
        a1 = (b - t) * t
        x1 = (b + t) / 2
        y1 = t / 2
        # vertical flange
        a2 = h * t
        x2 = t / 2
        y2 = h / 2

        # fillet to minus of the horizontal flange
        a3 = CONST2 * r2**2
        x3 = b - CONST1 * r2
        y3 = t - CONST1 * r2

        # fillet to minus of the vertical flange
        a4 = CONST2 * r2**2
        x4 = t - CONST1 * r2
        y4 = h - CONST1 * r2

        # fillet to plus
        a5 = CONST2 * r1**2
        x5 = t + CONST1 * r1
        y5 = t + CONST1 * r1

        # mass center of the profile
        x_c = (a1 * x1 + a2 * x2 - a3 * x3 - a4 * x4 + a5 * x5) / (
            a1 + a2 - a3 - a4 + a5
        )
        y_c = (a1 * y1 + a2 * y2 - a3 * y3 - a4 * y4 + a5 * y5) / (
            a1 + a2 - a3 - a4 + a5
        )
        return self.get_translation(B=b, H=h, x_s=x_c, y_s=y_c)
