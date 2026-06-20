"""Catalog / Plant entry point. Geometry lives in BLD_FLRF_CL150/CUST_BLD_FLRF_CL150.py."""
from varmain.custom import *  # type: ignore

from BLD_FLRF_CL150.CUST_BLD_FLRF_CL150 import BLDFLRFCL150


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="BLD FLRF CL150",
    TooltipLong="Blind flange raised face ASME B16.5 Class 150",
    FirstPortEndtypes="FL",
    LengthUnit="mm",
    Ports="1",
)
def CUST_BLD_FLRF_CL150(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return BLDFLRFCL150(s, int(DN), add_ports=not preview)
