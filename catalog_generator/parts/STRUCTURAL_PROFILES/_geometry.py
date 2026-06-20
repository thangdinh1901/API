
import math
import primitives as prim

# Constants for UPN profiles
SLOPE_RATIO = 0.08
SLOPE_ANGLE_DEG = math.degrees(math.atan(SLOPE_RATIO))

def build_ipe_shape(
    s,
    b: float,
    h: float,
    t1: float,
    t2: float,
    r1: float,
    height: float,
) -> prim.ShapeObject:
    """Build IPE (European I-beam) geometry with parallel flanges"""
    # Create web (central vertical part)
    web = prim.Box(s, t1, h, height)
    
    # Create top and bottom flanges
    flange_top = prim.Box(s, b, t2, height).move(y=(h - t2) / 2)
    flange_bottom = prim.Box(s, b, t2, height).move(y=-(h - t2) / 2)
    
    # Add corner fillets between web and flanges
    fillets = []
    for x_pos in [-t1/2, t1/2]:
        for y_pos in [(h - 2*t2)/2, -(h - 2*t2)/2]:
            fillet = prim.Fillet(s, r1, height).move(x=x_pos, y=y_pos)
            fillets.append(fillet)
    
    # Combine all parts
    profile = web.combine([flange_top, flange_bottom] + fillets)
    return profile

def build_upe_shape(
    s,
    B: float,
    H: float,
    t_w: float,
    t_f: float,
    r: float,
    height: float,
) -> prim.ShapeObject:
    """Build UPE (European U-beam) geometry with parallel flanges"""
    web = prim.Box(s, t_w, H, height).move(
        x=-B / 2 + t_w / 2
    )
    flange_top = prim.Box(s, B, t_f, height).move(
        y=-H / 2 + t_f / 2
    )
    flange_bottom = prim.Box(s, B, t_f, height).move(
        y=H / 2 - t_f / 2
    )
    fillets = [
        prim.Fillet(s, r, height).move(
            x=-B / 2 + t_w, y=-H / 2 + t_f
        ),
        prim.Fillet(s, r, height)
        .rotateZ(-90)
        .move(x=-B / 2 + t_w, y=H / 2 - t_f),
    ]
    return web.combine([flange_top, flange_bottom, *fillets])

def build_upn_shape(
    s,
    B: float,
    H: float,
    t_w: float,
    t_f: float,
    r1: float,
    r2: float,
    height: float,
) -> prim.ShapeObject:
    """Build UPN (European U-beam) geometry with sloped inner flanges"""
    flange_length = B - t_w
    slope_leg_height = flange_length * SLOPE_RATIO
    t_f_rect = t_f - B / 2 * SLOPE_RATIO

    web = prim.Box(s, t_w, H, height).move(
        x=t_w / 2 - B / 2
    )

    def create_flange() -> prim.ShapeObject:
        o1 = prim.Box(s, flange_length, t_f_rect, height).move(
            x=flange_length / 2, y=t_f_rect / 2
        )
        o2 = prim.RightTriangle(
            s, flange_length, slope_leg_height, height
        ).move(y=t_f_rect)
        fillet_add = (
            prim.Fillet(s, r1, height, 90 + SLOPE_ANGLE_DEG)
            .rotateZ(-SLOPE_ANGLE_DEG)
            .move(y=t_f_rect + slope_leg_height)
        )
        fillet_sub = (
            prim.Fillet(s, r2, height, 90 + SLOPE_ANGLE_DEG)
            .rotateZ(180 - SLOPE_ANGLE_DEG)
            .move(x=flange_length, y=t_f_rect)
        )
        return o1.combine([o2, fillet_add]).subtract(fillet_sub)

    flanges = [
        create_flange().move(x=-B / 2 + t_w, y=-H / 2),
        create_flange()
        .rotateX(180)
        .move(x=-B / 2 + t_w, y=H / 2, z=height),
    ]
    profile = web.combine(flanges)

    return profile

def build_ipn_shape(
    s,
    B: float,
    H: float,
    t_w: float,
    t_f: float,
    r1: float,
    r2: float,
    height: float,
) -> prim.ShapeObject:
    """Build IPN (European I-beam) geometry with sloped inner flanges"""
    # This function can be implemented similarly to build_upn_shape, but with different dimensions and slopes
    o1 = build_upn_shape(s, B/2, H, t_w/2, t_f, r1, r2, height).move(B/4)
    o2 = build_upn_shape(s, B/2, H, t_w/2, t_f, r1, r2, height).rotateZ(180).move(-B/4)
    return o1.combine(o2)

def build_L_shape(
    s,
    b: float,
    h: float,
    t: float,
    r1: float,
    r2: float,
    height: float,
) -> prim.ShapeObject:
    """Build L-shaped profile geometry"""
    leg_hor = prim.Box(s, b, t, height).move(y=(t-h)/2)
    leg_ver = prim.Box(s, t, h, height).move(x=(t-b)/2)
    fillets_del =[
        prim.Fillet(s, r2, height).rotateZ(180).move(b/2, t-h/2),
        prim.Fillet(s, r2, height).rotateZ(180).move(t-b/2, h/2),
    ]
    fillet_add = prim.Fillet(s, r1, height).move(t-b/2, t-h/2)

    return leg_hor.combine([leg_ver, fillet_add]).subtract(fillets_del)