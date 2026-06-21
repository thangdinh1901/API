"""Catalog entry — geometry in LJ_RING_CL150_RF/CUST_LJ_RING_CL150_RF.py."""
from varmain.custom import *  # type: ignore

import catalog_params
from LJ_RING_CL150_RF.CUST_LJ_RING_CL150_RF import LJRINGCL150RF


@activate(  # type: ignore
    Group="Flange",
    TooltipShort="LJ Ring CL150 FF",
    TooltipLong="Lap-joint backing ring flat face ASME B16.5 Class 150",
    FirstPortEndtypes="FL,LAP",
    LengthUnit="mm",
    Ports="2",
)
def CUST_LJ_RING_CL150_RF(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    dn = catalog_params.resolve_catalog_dn(DN, **kw)
    return LJRINGCL150RF(s, dn, add_ports=not preview)
