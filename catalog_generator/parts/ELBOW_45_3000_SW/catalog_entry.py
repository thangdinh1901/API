"""Catalog / Plant entry point. Geometry: ELBOW_45_3000_SW/CUST_ELBOW_45_3000_SW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from ELBOW_45_3000_SW.CUST_ELBOW_45_3000_SW import ELBOW_45_3000_SW


@activate(  # type: ignore
    Group="Elbow",
    TooltipShort="ELBOW 45",
    TooltipLong="ELBOW 45 SW CL3000 ASTM A105N ASME B16.11",
    FirstPortEndtypes="SW,SW",
    LengthUnit="mm",
    Ports="2",
)
def CUST_ELBOW_45_3000_SW(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return ELBOW_45_3000_SW(s, pipe_sizes.resolve_dn(DN), add_ports=not preview)
