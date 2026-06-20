"""Catalog entry — geometry in STUBEND_LJ_A_SH_BW_SCH40/CUST_STUBEND_LJ_A_SH_BW_SCH40.py."""
from varmain.custom import *  # type: ignore

from STUBEND_LJ_A_SH_BW_SCH40.CUST_STUBEND_LJ_A_SH_BW_SCH40 import STUBENDLJASHBWSCH40


@activate(  # type: ignore
    Group="Fastener",
    TooltipShort="Stub End LJ A SH BW",
    TooltipLong="ASME B16.9 Type A short-pattern lap-joint stub end Sch-40",
    FirstPortEndtypes="LAP,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_STUBEND_LJ_A_SH_BW_SCH40(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return STUBENDLJASHBWSCH40(s, int(DN), add_ports=not preview)
