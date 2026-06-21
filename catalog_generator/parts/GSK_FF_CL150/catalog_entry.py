"""Catalog / Plant entry point. Geometry lives in GSK_FF_CL150/CUST_GSK_FF_CL150.py."""
from varmain.custom import *  # type: ignore

import catalog_params
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
    dn = catalog_params.resolve_catalog_dn(DN, **kw)
    thickness = catalog_params.resolve_catalog_float("T", T, default_value=1.5, **kw)
    return GSKFFCL150(s, dn, thickness=thickness, add_ports=not preview)
