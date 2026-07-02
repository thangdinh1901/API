"""Catalog / Plant entry point. Geometry: REDUCER_ECC_STD_BW/CUST_REDUCER_ECC_STD_BW.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from REDUCER_ECC_STD_BW.CUST_REDUCER_ECC_STD_BW import REDUCER_ECC_STD_BW


@activate(  # type: ignore
    Group="Reducer",
    TooltipShort="REDUCER ECC",
    TooltipLong="REDUCER ECC BV CS SCHSTD ASTM A234-WPB ASME B16.9",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_REDUCER_ECC_STD_BW(s, DN=100, DN2=80, **kw):
    preview = bool(kw.get("preview", False))
    return REDUCER_ECC_STD_BW(s, pipe_sizes.resolve_dn(DN), size2=DN2, add_ports=not preview)
