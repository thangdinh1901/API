"""Catalog entry — geometry in TEE_EQ_SW_CL3000/CUST_TEE_EQ_SW_CL3000.py."""
from varmain.custom import *  # type: ignore

from TEE_EQ_SW_CL3000.CUST_TEE_EQ_SW_CL3000 import TEEEQSWCL3000


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Tee Equal SW",
    TooltipLong="ASME B16.11 Class 3000 socket weld equal tee",
    FirstPortEndtypes="SW,SW,SW",
    LengthUnit="mm",
    Ports="3",
)
def CUST_TEE_EQ_SW_CL3000(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return TEEEQSWCL3000(s, int(DN), add_ports=not preview)
