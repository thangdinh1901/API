from varmain.custom import *  # type: ignore

from THANG_TEST.CUST_THANG_TEST import THANG_TEST


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="THANG_TEST",
    TooltipLong="THANG_TEST",
    FirstPortEndtypes="BV,BV",
    LengthUnit="mm",
    Ports="2",
)
def CUST_THANG_TEST(s, DN=50, **kw):
    preview = bool(kw.get("preview", False))
    return THANG_TEST(s, int(DN), add_ports=not preview)
