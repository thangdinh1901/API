"""Catalog / Plant entry point. Geometry: FLANGE_LJ_RF_CL150/CUST_FLANGE_LJ_RF_CL150.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from FLANGE_LJ_RF_CL150.CUST_FLANGE_LJ_RF_CL150 import FLANGE_LJ_RF_CL150


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="FLANGE LJ",
    TooltipLong="FLANGE LJ_RF_CL150",
    FirstPortEndtypes="FL,LAP",
    LengthUnit="mm",
    Ports="2",
)
def CUST_FLANGE_LJ_RF_CL150(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return FLANGE_LJ_RF_CL150(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
