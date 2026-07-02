"""Catalog / Plant entry point. Geometry: TEE_EQUAL_SCH40_BW/CUST_TEE_EQUAL_SCH40_BW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from TEE_EQUAL_SCH40_BW.CUST_TEE_EQUAL_SCH40_BW import TEE_EQUAL_SCH40_BW


@activate(  # type: ignore
    Group="Tee",
    TooltipShort="TEE EQUAL",
    TooltipLong="TEE EQUAL BV CS SCH40 ASTM A234-WPB ASME B16.9",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="3",
)
def CUST_TEE_EQUAL_SCH40_BW(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return TEE_EQUAL_SCH40_BW(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
