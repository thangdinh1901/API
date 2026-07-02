"""Catalog / Plant entry point. Geometry: FLANGE_WN_CL150_RF/CUST_FLANGE_WN_CL150_RF.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from FLANGE_WN_CL150_RF.CUST_FLANGE_WN_CL150_RF import FLANGE_WN_CL150_RF


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="FLANGE WN",
    TooltipLong="FLANGE WN FL RF CL150 CS ASTM A105N ASME B16.5",
    FirstPortEndtypes="FL,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_FLANGE_WN_CL150_RF(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return FLANGE_WN_CL150_RF(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
