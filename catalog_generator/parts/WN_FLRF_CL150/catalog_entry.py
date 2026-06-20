"""Catalog / Plant entry point. Geometry lives in WN_FLRF_CL150/CUST_WN_FLRF_CL150.py."""
from varmain.custom import *  # type: ignore

from WN_FLRF_CL150.CUST_WN_FLRF_CL150 import WNFLRFCL150


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="WN FLRF CL150",
    TooltipLong="Weld neck flange raised face ASME B16.5 Class 150",
    FirstPortEndtypes="FL,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_WN_FLRF_CL150(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return WNFLRFCL150(s, int(DN), add_ports=not preview)
