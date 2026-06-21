"""Catalog entry — geometry in TEE_REDUCE_SW_CL3000/CUST_TEE_REDUCE_SW_CL3000.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from TEE_REDUCE_SW_CL3000.CUST_TEE_REDUCE_SW_CL3000 import TEEREDUCESWCL3000


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Tee Reduce SW",
    TooltipLong="ASME B16.11 Class 3000 reducing socket weld tee",
    FirstPortEndtypes="SW,SW,SW",
    LengthUnit="mm",
    Ports="3",
)
def CUST_TEE_REDUCE_SW_CL3000(s, DN=50, DN2=40, **kw):
    preview = bool(kw.get("preview", False))
    dn_r = int(DN)
    dn_b = int(DN2)
    if dn_b >= dn_r:
        dn_b = pipe_sizes.default_sw_tee_small_dn(dn_r)
    return TEEREDUCESWCL3000(s, dn_r, dn_b, add_ports=not preview)
