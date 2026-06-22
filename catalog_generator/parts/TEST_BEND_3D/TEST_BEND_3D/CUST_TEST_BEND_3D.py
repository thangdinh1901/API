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

class TEST_BEND_3D(ShapeObject):
    def __init__(self, s, DN=50, *, add_ports=True):
        # --- geometry (scene graph) ---
        elb_001 = Elbow(s, diameter=60.3, bend_radius=76, angle=90)
        wn_001 = WNFLRFCL150(s, 50, add_ports=False)
        wn_001 = wn_001.apply_matrix_rotation([0, 0, -1, 0, 1, 0, 1, 0, 0])
        wn_001 = wn_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_001 = wn_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_001 = wn_001.move(x=0, y=-0, z=213.5)
        wn_001 = wn_001.apply_matrix_rotation([0, 0, 1, 0, 1, 0, -1, 0, 0])
        wn_002 = WNFLRFCL150(s, 50, add_ports=False)
        wn_002 = wn_002.apply_matrix_rotation([0, 0, -1, 0, 1, 0, 1, 0, 0])
        wn_002 = wn_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_002 = wn_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_002 = wn_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        wn_002 = wn_002.move(x=0, y=213.5, z=0)
        wn_002 = wn_002.apply_matrix_rotation([0, 0, 1, 0, 1, 0, -1, 0, 0])
        elb_001 = elb_001.combine([wn_001])
        elb_001 = elb_001.combine([wn_002])
        geom = elb_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Auto-generated from Port Manager (scene graph)."""
        # Port 1 (FL)
        prim.set_port(
            s,
            prim.Point3d(0, 212, -0),
            prim.Point3d(-0, 1, 0),
        )
        # Port 2 (FL)
        prim.set_port(
            s,
            prim.Point3d(213.5, 0, -0),
            prim.Point3d(1, 0, -0),
        )
        return self
