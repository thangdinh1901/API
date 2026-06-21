"""Catalog / Plant entry point. Geometry lives in SO_FLRF_CL150/CUST_SO_FLRF_CL150.py."""
from varmain.custom import *  # type: ignore

import catalog_params
from SO_FLRF_CL150.CUST_SO_FLRF_CL150 import SOFLRFCL150


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="SO FLRF CL150",
    TooltipLong="Slip-on flange raised face ASME B16.5 Class 150",
    FirstPortEndtypes="FL,SO",
    LengthUnit="mm",
    Ports="2",
)
def CUST_SO_FLRF_CL150(s, DN=100, CEL=0.0, **kw):
    preview = bool(kw.get("preview", False))
    dn = catalog_params.resolve_catalog_dn(DN, **kw)
    cel = catalog_params.resolve_catalog_float("CEL", CEL, default_value=0.0, **kw)
    cel = None if cel in (0, 0.0) else float(cel)
    return SOFLRFCL150(s, dn, cel_mm=cel, add_ports=not preview)
