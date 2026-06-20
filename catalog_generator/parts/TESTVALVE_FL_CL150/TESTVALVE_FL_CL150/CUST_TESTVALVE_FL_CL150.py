# Port Manager: 2 connection point(s) — add_ports() below
import primitives as prim
from primitives import (
    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,
    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,
    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,
    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,
    ShapeObject,
)
from CUST_WN_FLRF_CL150 import WNFLRFCL150

class TESTVALVE_FL_CL150(ShapeObject):
    def __init__(self, s, DN=50, *, add_ports=True):
        # --- geometry (scene graph) ---
        cyl_001 = Cylinder(s, diameter=60.3, height=120)
        cyl_001 = cyl_001.rotateY(90)
        cyl_001 = cyl_001.move(x=-60, y=0, z=0)
        wn_001 = WNFLRFCL150(s, 50, add_ports=False)
        wn_001 = wn_001.move(x=-123.5, y=0, z=0)
        wn_002 = WNFLRFCL150(s, 50, add_ports=False)
        wn_002 = wn_002.apply_matrix_rotation([0, 0, -1, 0, 1, 0, 1, 0, 0])
        wn_002 = wn_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_002 = wn_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_002 = wn_002.move(x=-0, y=-0, z=123.5)
        wn_002 = wn_002.apply_matrix_rotation([0, 0, 1, 0, 1, 0, -1, 0, 0])
        cone_001 = Cone(s, bottom_diameter=60.3, height=87, top_diameter=30.15, eccentricity=1)
        sph_001 = Sphere(s, radius=50)
        cyl_001 = cyl_001.combine([wn_001])
        cyl_001 = cyl_001.combine([wn_002])
        cyl_001 = cyl_001.combine([cone_001])
        cyl_001 = cyl_001.combine([sph_001])
        geom = cyl_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Auto-generated from Port Manager (scene graph)."""
        # Port 1 (FL)
        prim.set_port(
            s,
            prim.Point3d(122, 0, 0),
            prim.Point3d(1, 0, 0),
        )
        # Port 2 (FL)
        prim.set_port(
            s,
            prim.Point3d(-123.5, 0, 0),
            prim.Point3d(-1, 0, 0),
        )
        return self
