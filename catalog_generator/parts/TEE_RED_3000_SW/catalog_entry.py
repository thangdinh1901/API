"""Catalog / Plant entry point. Geometry: TEE_RED_3000_SW/CUST_TEE_RED_3000_SW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from TEE_RED_3000_SW.CUST_TEE_RED_3000_SW import TEE_RED_3000_SW


@activate(  # type: ignore
    Group="Tee",
    TooltipShort="TEE RED",
    TooltipLong="TEE RED SW CL3000 ASTM A105N ASME B16.11",
    FirstPortEndtypes="SW,SW",
    LengthUnit="mm",
    Ports="3",
)
def CUST_TEE_RED_3000_SW(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return TEE_RED_3000_SW(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
