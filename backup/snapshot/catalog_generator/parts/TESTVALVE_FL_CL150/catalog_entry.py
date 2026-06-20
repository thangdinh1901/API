from varmain.custom import *  # type: ignore

from TESTVALVE_FL_CL150.CUST_TESTVALVE_FL_CL150 import TESTVALVE_FL_CL150


@activate(  # type: ignore
    Group="Valve",
    TooltipShort="TESTVALVE_FL_CL150",
    TooltipLong="TESTVALVE_FL_CL150",
    FirstPortEndtypes="FL,FL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_TESTVALVE_FL_CL150(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return TESTVALVE_FL_CL150(s, int(DN), add_ports=not preview)
