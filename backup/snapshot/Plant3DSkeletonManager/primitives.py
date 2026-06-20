from typing import cast
from varmain.primitiv import *  # type: ignore
from varmain.custom import *  # type: ignore
import math


def _shape(label: str, fn, *args, **kwargs):
    """Create a primitive via Plant 3D and raise if construction fails or returns nothing."""
    try:
        obj = fn(*args, **kwargs)
    except Exception as exc:
        raise RuntimeError(f"{label} failed: {exc}") from exc
    if not obj:
        raise RuntimeError(f"{label} returned no geometry")
    return obj

class Point3d:
    """Lightweight 3D point container for Plant 3D API calls."""
    def __init__(self, x=0.0, y=0.0, z=0.0):
        self.x, self.y, self.z = x, y, z

    def as_tuple(self):
        return (self.x, self.y, self.z)

def set_port(s, point: Point3d, direction: Point3d):
    """Define a Plant 3D port with origin and direction vectors."""
    s.setPoint(point.as_tuple(), direction.as_tuple())

def set_dimension(s, name: str, start: Point3d, end: Point3d):
    """Create a linear dimension between two points with a given name."""
    s.setLinearDimension(name, start.as_tuple(), end.as_tuple())

class ShapeObject:
    """Wrapper adding fluent transforms and Boolean ops around Plant 3D objects."""
    def __init__(self, obj):
        self.obj = obj

    def move(self, x=0.0, y=0.0, z=0.0):
        """Translate by offsets along X/Y/Z; returns self for chaining."""
        self.obj.translate((x, y, z))
        return self

    def combine(self, others):
        if not isinstance(others, list):
            others = [others]
        for other in others:
            self.obj.uniteWith(other.obj)
            other.erase()
        return self

    def erase(self):
        """Erase the object from the scene."""
        self.obj.erase()

    def subtract(self, others):
        """Subtract other object(s) from this object."""
        if not isinstance(others, list):
            others = [others]
        for other in others:
            self.obj.subtractFrom(other.obj)
            other.erase()
        return self

    def rotateX(self, angle: float):
        self.obj.rotateX(angle)
        return self

    def rotateY(self, angle: float):
        self.obj.rotateY(angle)
        return self

    def rotateZ(self, angle: float):
        self.obj.rotateZ(angle)
        return self

    def apply_matrix_rotation(self, rot9) -> "ShapeObject":
        """Apply scene-graph 3x3 row-major rotation (Plant 3D Z→Y→X, WCS at pivot)."""
        if rot9 is None or len(rot9) < 9:
            return self
        r00, r01, r02, r10, r11, r12, r20, r21, r22 = rot9
        sy = math.sqrt(r00 * r00 + r10 * r10)
        if sy > 1e-8:
            x_deg = math.degrees(math.atan2(r21, r22))
            y_deg = math.degrees(math.atan2(-r20, sy))
            z_deg = math.degrees(math.atan2(r10, r00))
        else:
            x_deg = math.degrees(math.atan2(-r12, r11))
            y_deg = math.degrees(math.atan2(-r20, sy))
            z_deg = 0.0
        if abs(z_deg) > 1e-6:
            self.rotateZ(z_deg)
        if abs(y_deg) > 1e-6:
            self.rotateY(y_deg)
        if abs(x_deg) > 1e-6:
            self.rotateX(x_deg)
        return self

    def intersect(self, other):
        """Intersect with another shape and return the resulting ShapeObject."""
        result = self.obj.intersectWith(other.obj)
        if not result:
            raise RuntimeError("Intersection failed: no geometry returned.")
        return ShapeObject(result)
    
    def cut_out_sector(self, s, radius: float, height: float, angle_start=0.0, angle_end=0.0):
        EPS = 1e-6
        sweep = (angle_end - angle_start) % 360.0

        if sweep < EPS:
            return self
        if abs(sweep - 360.0) < EPS:
            raise ValueError("Invalid sector angles: full 360° cut is not allowed.")

        sector = Cylinder(s, diameter=radius * 2, height=height)

        current_end = angle_start + sweep   
        remaining = sweep                  

        while remaining >= 90.0:
            cut_box = (
                Box(s, radius * 2, radius * 2, height)
                .move(x=radius, y=radius)
                .rotateZ(current_end - 90.0)
            )
            sector.subtract(cut_box)
            remaining -= 90.0
            current_end -= 90.0

        if remaining > EPS:
            cut_box = Box(s, radius * 2, radius * 2, height).move(x=radius, y=radius)
            cut_box2 = (
                Box(s, radius * 2, radius * 2, height)
                .move(x=radius, y=radius)
                .rotateZ(remaining)
            )
            cut_box.subtract(cut_box2)
            cut_box.rotateZ(angle_start)
            sector.subtract(cut_box)

        self.subtract(sector)
        return self

    def set_color(self, color_index: int):
        """Set the object's color by index (0-255); returns self for chaining."""
        self.obj.setColor(color_index)
        return self

class ShapeAssembly(ShapeObject):
    """Composite of multiple ShapeObjects; supports combined transforms and Boolean ops."""
    def __init__(self, *sub_objects: ShapeObject ):
        if not sub_objects:
            raise ValueError("ShapeAssembly requires at least one sub-object.")
        self.sub_objects = list(sub_objects)
        super().__init__(self.sub_objects[0].obj)
    
    def move(self, x=0.0, y=0.0, z=0.0):
        for obj in self.sub_objects:
            obj.move(x, y, z)
        return self
    def rotateX(self, angle: float):
        for obj in self.sub_objects:
            obj.rotateX(angle)
        return self
    def rotateY(self, angle: float):
        for obj in self.sub_objects:
            obj.rotateY(angle)
        return self
    def rotateZ(self, angle: float):
        for obj in self.sub_objects:
            obj.rotateZ(angle)
        return self

    def apply_matrix_rotation(self, rot9) -> "ShapeObject":
        for obj in self.sub_objects:
            obj.apply_matrix_rotation(rot9)
        return self

    def set_color(self, color_index: int):
        for obj in self.sub_objects:
            obj.set_color(color_index)
        return self
   
    def make_union(self):
        """Combine all sub-objects into a single unified shape."""
        if not self.sub_objects:
            raise RuntimeError("No sub-objects to combine.")
        base = self.sub_objects[0]
        for obj in self.sub_objects[1:]:
            base.combine(obj)
        self.obj = base.obj
        return self

class Box(ShapeObject):
    """Box aligned to axes; bottom on XY plane and centered."""

    def __init__(self, s, length: float, width: float, height: float):
        o1 = ShapeObject(
            _shape("BOX", BOX, s, H=length, L=width, W=height)  # type: ignore
        ).move(
            z=height / 2
        )
        super().__init__(o1.obj)

class Cylinder(ShapeObject):
    """Right or elliptical cylinder along +Z (optional wall thickness)."""
    def __init__(
        self,
        s,
        diameter: float,
        height: float,
        wall_thickness: float = 0.0,
        ellipse_diameter: float | None = None,
    ):
        if ellipse_diameter is None:
            o1 = _shape(
                "CYLINDER",
                CYLINDER,  # type: ignore
                s,
                R=diameter / 2,
                H=height,
                O=diameter / 2 - wall_thickness,
            )
        else:
            o1 = _shape(
                "CYLINDER",
                CYLINDER,  # type: ignore
                s,
                R1=diameter / 2,
                R2=ellipse_diameter / 2,
                H=height,
                O=diameter / 2 - wall_thickness,
            )
        super().__init__(o1)
        
class Cone(ShapeObject):
    """Cone or truncated cone along +Z."""
    def __init__(
        self,
        s,
        bottom_diameter: float,
        height: float,
        top_diameter=0.0,
        eccentricity=0.0,
    ):
        o1 = _shape(
            "CONE",
            CONE,  # type: ignore
            s,
            R1=bottom_diameter / 2,
            R2=top_diameter / 2,
            H=height,
            E=eccentricity,
        )
        super().__init__(o1)

class Torus(ShapeObject):
    """Torus lying in XY plane with major/minor radii derived from diameter/thickness."""
    def __init__(self, s, diameter: float, thickness: float):
        o1 = _shape(
            "TORUS",
            TORUS,  # type: ignore
            s,
            R1=diameter / 2,
            R2=thickness / 2,
        )
        super().__init__(o1)

class HalfSphere(ShapeObject):
    """Hemisphere; base lies in XY plane."""

    def __init__(self, s, radius: float):
        obj = _shape("HALFSPHERE", HALFSPHERE, s, R=radius)  # type: ignore
        super().__init__(obj)

class Reduced_elbow(ShapeObject):
    """Reducer elbow with differing inlet/outlet diameters."""
    def __init__(self, s, diameter1: float, diameter2: float, bend_radius: float, angle: float):
        EPS = 1e-6

        if bend_radius <= EPS:
            raise ValueError("Bend radius must be greater than zero.")
        if angle <= EPS or angle > 360.0 + EPS:
            raise ValueError("Angle must be in the range (0, 360].")
        
        if abs(angle - 180.0) < EPS:
            self.centerX = bend_radius
        else:
            denom_guard = math.cos(math.radians(angle / 2.0))
            if abs(denom_guard) < 1e-8:
                self.centerX = bend_radius
            else:
                self.centerX = bend_radius * math.tan(math.radians(angle / 2.0))
                
        self.centerY = bend_radius
        o1 = _shape(
            "ARC3D2",
            ARC3D2,  # type: ignore
            s,
            D=diameter1/2,
            D2=diameter2/2,
            R=bend_radius,
            A=angle,
        )
        super().__init__(o1)

class Elbow(ShapeObject):
    """Standard elbow with equal inlet/outlet diameters."""

    def __init__(self, s, diameter: float, bend_radius: float, angle: float):
        EPS = 1e-6

        if bend_radius <= EPS:
            raise ValueError("Bend radius must be greater than zero.")
        if angle <= EPS or angle > 360.0 + EPS:
            raise ValueError("Angle must be in the range (0, 360].")

        if abs(angle - 180.0) < EPS:
            self.centerX = bend_radius
        else:
            denom_guard = math.cos(math.radians(angle / 2.0))
            if abs(denom_guard) < 1e-8:
                self.centerX = bend_radius
            else:
                self.centerX = bend_radius * math.tan(math.radians(angle / 2.0))

        self.centerY = bend_radius 

        o1 = _shape(
            "ARC3D",
            ARC3D,  # type: ignore
            s,
            D=diameter/2.0,
            R=bend_radius,
            A=angle,
        )
        super().__init__(o1)

class SegmentedElbow(ShapeObject):
    """Segmented elbow built from discrete arc segments."""
    def __init__(self, s, diameter: float, bend_radius: float, angle: float, segments: int):
        EPS = 1e-6

        if bend_radius <= EPS:
            raise ValueError("Bend radius must be greater than zero.")
        if angle <= EPS or angle > 360.0 + EPS:
            raise ValueError("Angle must be in the range (0, 360].")
        if segments < 1:
            raise ValueError("Segments must be at least 1.")

        if abs(angle - 180.0) < EPS:
            self.centerX = bend_radius
        else:
            denom_guard = math.cos(math.radians(angle / 2.0))
            if abs(denom_guard) < 1e-8:
                self.centerX = bend_radius
            else:
                self.centerX = bend_radius * math.tan(math.radians(angle / 2.0))

        self.centerY = bend_radius 

        o1 = _shape(
            "ARC3DS",
            ARC3DS,  # type: ignore
            s,
            D=diameter/2.0,
            R=bend_radius,
            A=angle,
            S=segments,
        )
        super().__init__(o1)

class EllipsoidHead(ShapeObject):
    """Ellipsoidal head primitive with stored height parameter."""
    def __init__(self, s, diameter: float):
        o1 = _shape("ELLIPSOIDHEAD", ELLIPSOIDHEAD, s, R=diameter/2)  # type: ignore
        params = o1.parameters()
        self.height = float(params["H"])
        super().__init__(o1)

class EllipsoidHead2(ShapeObject):
    """Alternate ellipsoidal head variant exposing computed height."""
    def __init__(self, s, diameter: float):
        o1 = _shape("ELLIPSOIDHEAD2", ELLIPSOIDHEAD2, s, R=diameter/2)  # type: ignore
        params = o1.parameters()
        self.height = float(params["H"])
        super().__init__(o1)        

class EllipsoidSegment(ShapeObject):
    """Ellipsoidal shell segment with configurable rotations and span."""
    def __init__(self, s, radius_X: float, radius_Y: float, angle: float, 
                 rotation_start = 0.0, angle_start=0.0, angle_end=360.0):
        o1 = _shape(
            "ELLIPSOIDSEGMENT",
            ELLIPSOIDSEGMENT,  # type: ignore
            s,
            RX=radius_X,
            RY=radius_Y,
            A1=angle,
            A2=rotation_start,
            A3=angle_start,
            A4=angle_end,
        )
        super().__init__(o1)
        
class Pyramid(ShapeObject):
    """Frustum pyramid; HT allows full-height pyramids when > 0."""
    def __init__(self, s, base_length: float, base_width: float, frustum_height: float, total_height = 0.0):
        o1 = _shape(
            "PYRAMID",
            PYRAMID,  # type: ignore
            s,
            L=base_length,
            W=base_width,
            H=frustum_height,
            HT=total_height,
        )
        super().__init__(o1)
        
class RoundRectangle(ShapeObject):
    """Rectangular prism with filleted edges defined by diameter."""
    def __init__(self, s, base_length: float, base_width: float, height: float, diam: float,
                 eccentricity: float = 0.0):
        o1 = _shape("ROUNDRECT", ROUNDRECT, s, L=base_length, W=base_width, H=height, R2=diam/2, E=eccentricity)  # type: ignore
        super().__init__(o1)

class SphereSegment(ShapeObject):
    """Spherical segment cut to a given height, optional offset."""
    def __init__(self, s, radius: float, segment_height: float, start_height: float = 0.0):
        o1 = _shape("SPHERESEGMENT", SPHERESEGMENT, s, R=radius, H=segment_height, SH=start_height)  # type: ignore
        super().__init__(o1)
        
class TorisPhericHead(ShapeObject):
    """Torispherical head with stored computed height."""
    def __init__(self, s, diameter: float):
        o1 = _shape("TORISPHERICHEAD", TORISPHERICHEAD, s, R=diameter/2)  # type: ignore
        params = o1.parameters()
        self.height = float(params["H"])
        super().__init__(o1)

class TorisPhericHead2(ShapeObject):
    """Alternate torispherical head variant with exposed height."""
    def __init__(self, s, diameter: float):
        o1 = _shape("TORISPHERICHEAD2", TORISPHERICHEAD2, s, R=diameter/2)  # type: ignore
        params = o1.parameters()
        self.height = float(params["H"])
        super().__init__(o1)
        
class TorisPhericHeadH(ShapeObject):
    """Torispherical head defined by diameter and explicit height."""
    def __init__(self, s, diameter: float, height: float):
        o1 = _shape("TORISPHERICHEADH", TORISPHERICHEADH, s, R=diameter/2, H=height)  # type: ignore
        super().__init__(o1)

class TorusSector(ShapeObject):
    """Torus section trimmed by start/end angles."""
    def __init__(self, s, diameter, thickness, angle_start=0.0, angle_end=0.0):
        """
        **Creates a torus sector by cutting out a piece of geometry from a torus.**

        **Parameters:**
            - *s*:  Shape object from Plant 3D (required).
            - *diameter* (`float`): The outer diameter of the torus.
            - *thickness* (`float`): The cross-sectional thickness of the torus.
            - *angle_start* (`float`, optional): Start angle (degrees) of the sector. *Default: 0.0*
            - *angle_end* (`float`, optional): End angle (degrees) of the sector. *Default: 0.0*

        **Behavior:**
            - If *angle_start* and *angle_end* are equal, the full torus is created (no sector cut).
            - The torus lies in the XY plane, centered at the origin.

        **Raises:**
            - Any exceptions from the `Torus` or `cut_sector` methods.
        """
        
        base = Torus(s, diameter, thickness).move(z=thickness / 2)
        if angle_end != angle_start:
            base.cut_out_sector(
                    s,
                    radius=diameter,
                    height=thickness,
                    angle_start=angle_start,
                    angle_end=angle_end,
                )
        base.move(z=-thickness / 2)
            
        super().__init__(base.obj)

class CylinderSector(ShapeObject):
    """Cylindrical shell trimmed by angular sector."""
    def __init__(
        self,
        s,
        diameter: float,
        height: float,
        wall_thickness: float = 0.0,
        angle_start=0.0,
        angle_end=360.0,
    ):
               
        o1 = Cylinder(
            s, diameter=diameter, height=height, wall_thickness=wall_thickness
        ).cut_out_sector(s, diameter / 2, height, angle_start, angle_end)
        super().__init__(o1.obj)

class CylinderChamfered(ShapeObject):
    """Cylinder with single or double chamfered ends."""
    def __init__(
        self,
        s,
        diameter: float,
        height: float,
        chamfer: float = 0.0,
        chamfer_angle: float = 45.0,
        double_chamfer=False,
    ):
        height_chamfer = chamfer / math.tan(math.radians(chamfer_angle))
        small_d = diameter - 2 * chamfer
        if double_chamfer:
            o1 = Cone(
                s,
                bottom_diameter=small_d,
                height=height_chamfer,
                top_diameter=diameter,
            )
            o2 = Cylinder(
                s, diameter=diameter, height=height - 2 * height_chamfer
            ).move(z=height_chamfer)
            o3 = Cone(
                s,
                bottom_diameter=diameter,
                height=height_chamfer,
                top_diameter=small_d,
            ).move(z=height - height_chamfer)
            o1.combine([o2, o3])
        else:
            o1 = Cone(
                s,
                bottom_diameter=small_d,
                height=height_chamfer,
                top_diameter=diameter,
            )
            o2 = Cylinder(
                s, diameter=diameter, height=height - height_chamfer
            ).move(z=height_chamfer)
            o1.combine(o2)
        super().__init__(o1.obj)

class Fillet(ShapeObject):
    def __init__(self, s, radius=0.0, height=0.0, angle=90.0):
        """
        Creates a fillet shape object.

        Parameters:
            s (object): The shape object from Plant 3D.
            radius (float): The radius of the fillet. Must be > 0.
            height (float): The height of the fillet. Must be > 0.
            angle (float): The angle of the fillet in degrees. Must be > 0 and <= 180.
        Raises:
            ValueError: If any parameter is out of range.
        """
        if radius <= 0:
            raise ValueError("Fillet: radius must be > 0.")
        if height <= 0:
            raise ValueError("Fillet: height must be > 0.")
        if angle <= 0 or angle > 180:
            raise ValueError("Fillet: angle must be in (0, 180].")

        main_body_radius = radius / math.tan(math.radians(angle / 2))
        o1 = CylinderSector(
            s, main_body_radius * 2, height, angle_start=0, angle_end=angle
        )
        o2 = Cylinder(s, diameter=radius * 2, height=height).move(
            x=main_body_radius, y=radius
        )
        o1.subtract(o2)
        super().__init__(o1.obj)

class BoxWithFillet(ShapeObject):
    """Box with filleted corners; controlled count via number_of_fillets."""
    def __init__(
        self,
        s,
        length: float,
        width: float,
        height: float,
        radius: float,
        number_of_fillets = 4,
    ):
        fillets = []

        quadrants = [
            (-length/2, -width/2),
            (length/2, -width/2),
            (length/2, width/2),
            (-length/2, width/2),
        ]
        base = Box(s, length, width, height)
        for i in range(number_of_fillets):
            fillets.append(
                Fillet(s, radius, height).rotateZ(i * 90).move(
                    x=quadrants[i][0], y=quadrants[i][1]))
        base.subtract(fillets)
        super().__init__(base.obj)

class RightTriangle(ShapeObject):
    """Right triangular prism defined by legs and height."""
    def __init__(self, s, leg_base: float, leg_height: float, height: float):

        o1 = Box(s, leg_base, leg_height, height).move(
            x=leg_base / 2, y=leg_height / 2
        )

        hypotenuse = math.hypot(leg_base, leg_height)

        cut_box = (
            Box(s, hypotenuse, hypotenuse, height)
            .move(x=hypotenuse / 2, y=hypotenuse / 2)
            .rotateZ(-math.degrees(math.atan2(leg_height, leg_base)))
            .move(y=leg_height)
        )
        o1.subtract(cut_box)
        super().__init__(o1.obj)

    def from_base_and_angle(self, s, leg_base: float, angle: float, height: float):

        angle_rad = math.radians(angle)
        leg_height = leg_base * math.tan(angle_rad)

        return RightTriangle(s, leg_base, leg_height, height)

class Sphere(ShapeObject):
    """Full sphere centered at base point; bottom touches XY plane."""

    def __init__(self, s, radius):
        o1 = HalfSphere(s, radius)
        o2 = HalfSphere(s, radius).rotateX(180)
        o1.combine(o2)
        super().__init__(o1.obj)

class CylinderWithFillet(ShapeObject):
    """
    Represents a cylinder with optional hemispherical or toroidal fillets.
    Axis: Z.
    If fillet_radius == diameter / 2 → hemispherical ends.
    If double_fillet=True → both ends are filleted.
    """

    def __init__(self, s, diameter: float, height: float,
                 fillet_radius: float = 0.0, double_fillet: bool = False):

        if fillet_radius < 0:
            raise ValueError("Fillet radius cannot be negative.")
        if fillet_radius > diameter / 2:
            raise ValueError("Fillet radius cannot exceed half the diameter.")

        # Simple cylinder (no fillet)
        if fillet_radius == 0:
            super().__init__(Cylinder(s, diameter, height).obj)
            return

        components: list[ShapeObject] = []

        if math.isclose(fillet_radius, diameter / 2):
            # Hemispherical ends
            main_h = height - (2 * fillet_radius if double_fillet else fillet_radius)
            core = Cylinder(s, diameter=diameter, height=main_h)
            components.append(core)

            # Top hemisphere
            components.append(HalfSphere(s, fillet_radius).move(z=height - fillet_radius))

            # Bottom hemisphere
            if double_fillet:
                components.append(
                    HalfSphere(s, fillet_radius).rotateX(180).move(z=fillet_radius)
                )

        else:
            # Toroidal fillets
            main_h = height - (2 * fillet_radius if double_fillet else fillet_radius)
            base = Cylinder(s, diameter=diameter, height=main_h)
            core = Cylinder(s, diameter - 2 * fillet_radius, height)
            if double_fillet:
                base.move(z=fillet_radius)
            components.extend([base, core])

            # Top fillet
            torus_top = Torus(s, diameter=diameter - 2 * fillet_radius,
                              thickness=2 * fillet_radius).move(z=height - fillet_radius)
            components.append(torus_top)

            # Bottom torus
            if double_fillet:
                torus_bot = Torus(s, diameter=diameter - 2 * fillet_radius,
                                  thickness=2 * fillet_radius).move(z=fillet_radius)
                components.append(torus_bot)

        result = cast(ShapeObject, components[0])
        result.combine(components[1:])
        super().__init__(result.obj)

class PrismTriangle(ShapeObject):
    """Triangular prism cutter defined by base edges or edge+angle."""

    def __init__(self, s, base1, height, base2=None, angle=None):
        """Construct prism from two bases or base+angle; useful for angular cuts."""
        if angle is not None:
            base2 = base1 * math.tan(math.radians(angle))
        if base2 is None:
            base2 = base1
        if angle is None:
            angle = math.degrees(math.atan(base2/base1))
        o1 = Box(s, base1, base2, height).move(
            x=base1/2, y=base2/2)
        o2 = Box(s, base1*2, base2*2,
                 height).move(x=base1, y=base2).rotateZ(90-angle).move(x=base1)
        o1.subtract(o2)
        # Call the parent constructor
        super().__init__(o1.obj)

#new custom primitives 
class BeanTorus(ShapeObject):
    """Bean-shaped torus created by intersecting two offset tori."""
    def __init__(self, s, diameter: float, thickness: float, offset: float):
        if offset < 0 or offset > diameter / 2:
            raise ValueError("Offset must be between 0 and half the diameter.")
        
        torus1 = Torus(s, diameter=diameter, thickness=thickness)
        cyl = Cylinder(s,diameter=diameter, height=thickness
                       ,wall_thickness=offset).move(z=-thickness/2)
        torus2 = Torus(s, diameter=diameter - 2 * offset, thickness=thickness)
        torus1.combine([cyl, torus2])
        super().__init__(torus1.obj)

class Spring(ShapeObject):

    """Helical spring created by sweeping a circular profile along a helical path."""

    def __init__(self, s, diameter: float, thickness: float, pitch: float, turns: int):
        if diameter <= 0:
            raise ValueError("Diameter must be greater than zero.")
        if thickness <= 0:
            raise ValueError("Thickness must be greater than zero.")
        if pitch <= 0:
            raise ValueError("Pitch must be greater than zero.")
        if turns <= 0:
            raise ValueError("Turns must be greater than zero.")
        if pitch == thickness:
            raise ValueError("Pitch must be greater than thickness to avoid self-intersection.")

        segment_diameter = math.hypot(pitch / 2, diameter / 2) * 2
        segment_rotation = math.degrees(math.atan(pitch / diameter))

        angle_ranges = [(0, 180), (180, 360)]
        rotation_signs = [-1, 1]

        coils: list[ShapeObject] = []

        for i in range(turns):
            idx = i % 2
            start_angle, end_angle = angle_ranges[idx]
            try:
                coil = (
                    TorusSector(s, segment_diameter, thickness, start_angle, end_angle)
                    .rotateY(rotation_signs[idx] * segment_rotation)
                    .move(z=i * pitch)
                )
            except Exception as e:
                raise RuntimeError(f"Error creating coil {i}: {e}")
            coils.append(coil)

        result = coils[0].combine(coils[1:])
        super().__init__(result.obj)

class Pigtail(ShapeObject):
    """Instrumentation pigtail loop installed in front of a pressure gauge."""

    def __init__(self, s, diameter: float, thickness: float, height: float):
        if diameter <= 0:
            raise ValueError("diameter must be greater than zero.")
        if thickness <= 0:
            raise ValueError("thickness must be greater than zero.")
        if height <= 0:
            raise ValueError("height must be greater than zero.")
        if thickness >= diameter / 2:
            raise ValueError("thickness is too large relative to diameter.")

        sin60 = math.sqrt(3) / 2
        y_move = diameter * sin60
        straight_len = (height - 2 * y_move) / 2

        if straight_len <= 0:
            raise ValueError(
                "height is too small for the given diameter. "
                f"Minimum height must be greater than {2 * y_move:.3f}."
            )

        # Slight oversizing factor to avoid coincident faces during boolean union
        segment_rotation = math.degrees(math.atan(thickness * 1.01 / diameter))

        def _half_loop():
            o1 = TorusSector(s, diameter, thickness, 180, 60)
            o2 = TorusSector(s, diameter, thickness, 180, 240).move(diameter / 2, y_move)
            o3 = Cylinder(s, thickness, straight_len).rotateX(-90).move(y=y_move)
            return o1.combine([o2, o3])

        loop1 = _half_loop().rotateX(90).rotateZ(-segment_rotation).move(y=-thickness)
        loop2 = _half_loop().rotateX(-90).rotateZ(segment_rotation)

        loop1.combine(loop2)
        super().__init__(loop1.obj)