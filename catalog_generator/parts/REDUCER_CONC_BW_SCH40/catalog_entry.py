"""Catalog entry — geometry in REDUCER_CONC_BW_SCH40/CUST_REDUCER_CONC_BW_SCH40.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from REDUCER_CONC_BW_SCH40.CUST_REDUCER_CONC_BW_SCH40 import REDUCERCONCBWSCH40


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Reducer Conc BW",
    TooltipLong="ASME B16.9 concentric butt weld reducer",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_REDUCER_CONC_BW_SCH40(s, DN=100, DN2=80, **kw):
    preview = bool(kw.get("preview", False))
    dn_l = int(DN)
    dn_s = int(DN2)
    if dn_s >= dn_l:
        dn_s = pipe_sizes.default_reducer_small_dn(dn_l)
    return REDUCERCONCBWSCH40(s, dn_l, dn_s, add_ports=not preview)
