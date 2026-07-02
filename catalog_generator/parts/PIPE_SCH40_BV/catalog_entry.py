"""Catalog / Plant entry point. Geometry: PIPE_SCH40_BV/CUST_PIPE_SCH40_BV.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from PIPE_SCH40_BV.CUST_PIPE_SCH40_BV import PIPE_SCH40_BV


@activate(  # type: ignore
    Group="Pipe",
    TooltipShort="PIPE",
    TooltipLong="PIPE BV SCH40 CS ASTM A53-B ASME B36.10",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_PIPE_SCH40_BV(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return PIPE_SCH40_BV(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
