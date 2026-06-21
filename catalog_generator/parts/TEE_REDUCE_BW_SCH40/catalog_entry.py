"""Catalog entry — geometry in TEE_REDUCE_BW_SCH40/CUST_TEE_REDUCE_BW_SCH40.py."""
from varmain.custom import *  # type: ignore

import pipe_sizes
from TEE_REDUCE_BW_SCH40.CUST_TEE_REDUCE_BW_SCH40 import TEEREDUCEBWSCH40


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Tee Reduce BW",
    TooltipLong="ASME B16.9 reducing butt weld tee",
    FirstPortEndtypes="BV,BV,BV",
    LengthUnit="mm",
    Ports="3",
)
def CUST_TEE_REDUCE_BW_SCH40(s, DN=100, DN2=80, **kw):
    preview = bool(kw.get("preview", False))
    dn_r = int(DN)
    dn_b = int(DN2)
    if dn_b >= dn_r:
        dn_b = pipe_sizes.default_reducer_small_dn(dn_r)
    return TEEREDUCEBWSCH40(s, dn_r, dn_b, add_ports=not preview)
