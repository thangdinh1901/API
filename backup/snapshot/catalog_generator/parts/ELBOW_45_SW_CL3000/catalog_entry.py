"""Catalog entry — geometry in ELBOW_45_SW_CL3000/CUST_ELBOW_45_SW_CL3000.py."""
from varmain.custom import *  # type: ignore

from ELBOW_45_SW_CL3000.CUST_ELBOW_45_SW_CL3000 import ELBOW45SWCL3000


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Elbow 45 SW",
    TooltipLong="ASME B16.11 Class 3000 socket weld 45 deg elbow",
    FirstPortEndtypes="SW,SW",
    LengthUnit="mm",
    Ports="2",
)
def CUST_ELBOW_45_SW_CL3000(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return ELBOW45SWCL3000(s, int(DN), add_ports=not preview)
