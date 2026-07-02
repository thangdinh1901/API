"""Catalog / Plant entry point. Geometry: STUB_END_LONG_SCH40_BW/CUST_STUB_END_LONG_SCH40_BW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from STUB_END_LONG_SCH40_BW.CUST_STUB_END_LONG_SCH40_BW import STUB_END_LONG_SCH40_BW


@activate(  # type: ignore
    Group="StubEnd",
    TooltipShort="STUB-END LONG",
    TooltipLong="STUB END_LONG_SCH40_BW",
    FirstPortEndtypes="LAP,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_STUB_END_LONG_SCH40_BW(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return STUB_END_LONG_SCH40_BW(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
