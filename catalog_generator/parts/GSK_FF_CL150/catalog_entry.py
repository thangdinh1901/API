"""Catalog / Plant entry point. Geometry lives in GSK_FF_CL150/CUST_GSK_FF_CL150.py."""
from varmain.custom import *  # type: ignore

from GSK_FF_CL150.CUST_GSK_FF_CL150 import GSKFFCL150


@activate(  # type: ignore
    Group="Gasket",
    TooltipShort="GSK FF CL150 + studs",
    TooltipLong="Full-face FF gasket with lap-joint stud bolts ASME B16.5 Class 150 (visual)",
    FirstPortEndtypes="FL,FL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_GSK_FF_CL150(s, DN=100, T=1.5, **kw):
    preview = bool(kw.get("preview", False))
    return GSKFFCL150(s, int(DN), thickness=float(T), add_ports=not preview)
