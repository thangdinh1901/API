"""Catalog / Plant entry point. Geometry lives in SO_FLRF_CL150/CUST_SO_FLRF_CL150.py."""
from varmain.custom import *  # type: ignore

from SO_FLRF_CL150.CUST_SO_FLRF_CL150 import SOFLRFCL150


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="SO FLRF CL150",
    TooltipLong="Slip-on flange raised face ASME B16.5 Class 150",
    FirstPortEndtypes="FL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_SO_FLRF_CL150(s, DN=100, CEL=0.0, **kw):
    preview = bool(kw.get("preview", False))
    cel = None if CEL in (None, 0, 0.0) else float(CEL)
    return SOFLRFCL150(s, int(DN), cel_mm=cel, add_ports=not preview)
