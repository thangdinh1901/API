"""Catalog / Plant entry point. Geometry: BLIND_FLANGE_CL150_RF/CUST_BLIND_FLANGE_CL150_RF.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from BLIND_FLANGE_CL150_RF.CUST_BLIND_FLANGE_CL150_RF import BLIND_FLANGE_CL150_RF


@activate(  # type: ignore
    Group="BlindFlange",
    TooltipShort="BLIND FLANGE",
    TooltipLong="BLIND FLANGE FL RF CL150 CS ASTM A105N ASME B16.5",
    FirstPortEndtypes="FL,FL",
    LengthUnit="mm",
    Ports="1",
)
def CUST_BLIND_FLANGE_CL150_RF(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return BLIND_FLANGE_CL150_RF(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
