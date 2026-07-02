"""Catalog / Plant entry point. Geometry: FLANGE_SW_CL150_RF/CUST_FLANGE_SW_CL150_RF.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from FLANGE_SW_CL150_RF.CUST_FLANGE_SW_CL150_RF import FLANGE_SW_CL150_RF


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="FLANGE SW",
    TooltipLong="FLANGE SW FL RF CL150 CS ASTM A105N ASME B16.5",
    FirstPortEndtypes="FL,SW",
    LengthUnit="mm",
    Ports="2",
)
def CUST_FLANGE_SW_CL150_RF(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return FLANGE_SW_CL150_RF(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
