"""Catalog / Plant entry point. Geometry: FLANGE_SO_CL150_RF/CUST_FLANGE_SO_CL150_RF.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from FLANGE_SO_CL150_RF.CUST_FLANGE_SO_CL150_RF import FLANGE_SO_CL150_RF


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="FLANGE SO",
    TooltipLong="FLANGE SO FL RF CL150 CS ASTM A105N ASME B16.5",
    FirstPortEndtypes="FL,SO",
    LengthUnit="mm",
    Ports="2",
)
def CUST_FLANGE_SO_CL150_RF(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return FLANGE_SO_CL150_RF(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
