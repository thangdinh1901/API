from varmain.custom import *  # type: ignore

from BEND_3D_JACOB.CUST_BEND_3D_JACOB import BEND_3D_JACOB


@activate(  # type: ignore
    Group="Fitting",
    TooltipShort="BEND_3D_JACOB",
    TooltipLong="BEND_3D_JACOB",
    FirstPortEndtypes="FL,FL",
    LengthUnit="mm",
    Ports="2",
)
def CUST_BEND_3D_JACOB(s, DN=100, D=0, D2=0, R=0, A=0, L1=0, L2=0, OF=0, **kw):
    preview = bool(kw.get("preview", False))
    import pipe_sizes
    body_od = pipe_sizes.pipe_od_sch40_mm(pipe_sizes.resolve_dn(int(DN)))
    dim = {"BodyOD": 114.3}
    dim.setdefault("BodyOD", body_od)
    for _k, _v in kw.items():
        if _v in (None, ""):
            continue
        if _k == "L":
            dim["L"] = float(_v)
        elif _k == "D1":
            dim["BodyOD"] = float(_v)
        elif _k in dim:
            dim[_k] = float(_v)
    return BEND_3D_JACOB(s, int(DN), add_ports=not preview, **dim)
