"""Catalog entry — geometry in ELBOW_90_SW_CL3000/CUST_ELBOW_90_SW_CL3000.py."""
from varmain.custom import *  # type: ignore

from ELBOW_90_SW_CL3000.CUST_ELBOW_90_SW_CL3000 import ELBOW90SWCL3000


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Elbow 90 SW",
    TooltipLong="ASME B16.11 Class 3000 socket weld 90 deg elbow",
    FirstPortEndtypes="SW,SW",
    LengthUnit="mm",
    Ports="2",
)
def CUST_ELBOW_90_SW_CL3000(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return ELBOW90SWCL3000(s, int(DN), add_ports=not preview)
