"""Catalog entry — geometry in ELBOW_45_LR_BW_SCH40/CUST_ELBOW_45_LR_BW_SCH40.py."""
from varmain.custom import *  # type: ignore

from ELBOW_45_LR_BW_SCH40.CUST_ELBOW_45_LR_BW_SCH40 import ELBOW45LRBWSCH40


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Elbow 45 LR BW",
    TooltipLong="ASME B16.9 45 deg LR butt weld elbow",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_ELBOW_45_LR_BW_SCH40(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return ELBOW45LRBWSCH40(s, int(DN), add_ports=not preview)
