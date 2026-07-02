"""Catalog / Plant entry point. Geometry: ELBOW_90_SCH40_BW/CUST_ELBOW_90_SCH40_BW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from ELBOW_90_SCH40_BW.CUST_ELBOW_90_SCH40_BW import ELBOW_90_SCH40_BW


@activate(  # type: ignore
    Group="Elbow",
    TooltipShort="ELBOW 90",
    TooltipLong="ELBOW 90 BV SCH40 ASTM A234-WPB ASME B16.9",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_ELBOW_90_SCH40_BW(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return ELBOW_90_SCH40_BW(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
