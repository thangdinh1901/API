"""Catalog / Plant entry point. Geometry: PIPE_SCH80_PL/CUST_PIPE_SCH80_PL.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from PIPE_SCH80_PL.CUST_PIPE_SCH80_PL import PIPE_SCH80_PL


@activate(  # type: ignore
    Group="Pipe",
    TooltipShort="PIPE",
    TooltipLong="PIPE PL SCH80 CS ASTM A106-B ASME B36.10",
    FirstPortEndtypes="PL,PL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_PIPE_SCH80_PL(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return PIPE_SCH80_PL(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
