"""Catalog entry — geometry in TEE_EQ_BW_SCH40/CUST_TEE_EQ_BW_SCH40.py."""
from varmain.custom import *  # type: ignore

from TEE_EQ_BW_SCH40.CUST_TEE_EQ_BW_SCH40 import TEEEQBWSCH40


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Tee Equal BW",
    TooltipLong="ASME B16.9 equal butt weld tee",
    FirstPortEndtypes="BV,BV,BV",
    LengthUnit="mm",
    Ports="3",
)
def CUST_TEE_EQ_BW_SCH40(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return TEEEQBWSCH40(s, int(DN), add_ports=not preview)
