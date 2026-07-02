"""Catalog / Plant entry point. Geometry: REDUCER_CONC_STD_BW/CUST_REDUCER_CONC_STD_BW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from REDUCER_CONC_STD_BW.CUST_REDUCER_CONC_STD_BW import REDUCER_CONC_STD_BW


@activate(  # type: ignore
    Group="Reducer",
    TooltipShort="REDUCER CONC",
    TooltipLong="REDUCER CONC BV CS SCHSTD ASTM A234-WPB ASME B16.9",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_REDUCER_CONC_STD_BW(s, DN=100, DN2=80, **kw):
    preview = bool(kw.get("preview", False))
    return REDUCER_CONC_STD_BW(s, pipe_sizes.resolve_dn(DN), size2=DN2, add_ports=not preview)
