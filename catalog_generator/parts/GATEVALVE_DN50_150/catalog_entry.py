from varmain.custom import *  # type: ignore

from GATEVALVE_DN50_150.CUST_GATEVALVE_DN50_150 import GATEVALVE_DN50_150


@activate(  # type: ignore
    Group="Valve",
    TooltipShort="GATEVALVE_DN50_150",
    TooltipLong="GATEVALVE_DN50_150",
    FirstPortEndtypes="FL,FL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_GATEVALVE_DN50_150(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return GATEVALVE_DN50_150(s, int(DN), add_ports=not preview)
