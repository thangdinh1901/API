"""Catalog entry — geometry in ELBOW_90_SR_BW_SCH40/CUST_ELBOW_90_SR_BW_SCH40.py."""
from varmain.custom import *  # type: ignore

from ELBOW_90_SR_BW_SCH40.CUST_ELBOW_90_SR_BW_SCH40 import ELBOW90SRBWSCH40


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="Elbow 90 SR BW",
    TooltipLong="ASME B16.9 90 deg short radius butt weld elbow",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_ELBOW_90_SR_BW_SCH40(s, DN=100, **kw):
    preview = bool(kw.get("preview", False))
    return ELBOW90SRBWSCH40(s, int(DN), add_ports=not preview)
