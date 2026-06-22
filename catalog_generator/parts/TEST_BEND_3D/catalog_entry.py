from varmain.custom import *  # type: ignore

from TEST_BEND_3D.CUST_TEST_BEND_3D import TEST_BEND_3D


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="TEST_BEND_3D",
    TooltipLong="TEST_BEND_3D",
    FirstPortEndtypes="FL,FL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_TEST_BEND_3D(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return TEST_BEND_3D(s, int(DN), add_ports=not preview)
