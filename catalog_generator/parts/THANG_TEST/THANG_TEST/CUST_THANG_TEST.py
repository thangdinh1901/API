# Port Manager: 2 connection point(s) — add_ports() below
# P3D_SCENE_MANIFEST: 7 part(s): CYL_001, CONE_001, BFIL_001, BFIL_002, SO_001, SO_002, HSPH_001
import primitives as prim
from primitives import (
    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,
    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,
    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,
    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,
    BoxWithFillet, CylinderChamfered, CylinderWithFillet, Fillet,
    ShapeObject,
)
from CUST_SO_FLRF_CL150 import SOFLRFCL150

class THANG_TEST(ShapeObject):
    def __init__(self, s, DN=50, BodyOD=60.3, L=178, *, add_ports=True):
        # --- geometry (scene graph) ---
        cyl_001 = Cylinder(s, diameter=80, height=178)
        cyl_001 = cyl_001.rotateY(90)
        cyl_001 = cyl_001.move(x=-89, y=0, z=0)
        cone_001 = Cone(s, bottom_diameter=110, height=60, top_diameter=80, eccentricity=0)
        cone_001 = cone_001.rotateY(90)
        cone_001 = cone_001.rotateY(90)
        cone_001 = cone_001.rotateY(90)
        cone_001 = cone_001.rotateY(90)
        cone_001 = cone_001.rotateY(90)
        bfil_001 = BoxWithFillet(s, length=120, width=120, height=10, radius=10, number_of_fillets=4)
        bfil_001 = bfil_001.rotateY(90)
        bfil_001 = bfil_001.move(x=-10, y=0, z=0)
        bfil_002 = BoxWithFillet(s, length=120, width=120, height=10, radius=10, number_of_fillets=4)
        bfil_002 = bfil_002.rotateY(90)
        bfil_002 = bfil_002.move(x=-22, y=0, z=0)
        so_001 = SOFLRFCL150(s, 50, cel_mm=5, add_ports=False)
        so_001 = so_001.apply_matrix_rotation([0, 0, -1, 0, 1, 0, 1, 0, 0])
        so_001 = so_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_001 = so_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_001 = so_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_001 = so_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_001 = so_001.move(x=0, y=0, z=89)
        so_001 = so_001.move(x=-0, y=-0, z=-89)
        so_001 = so_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_001 = so_001.move(x=0, y=0, z=89)
        so_001 = so_001.move(x=-0, y=-0, z=-89)
        so_001 = so_001.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_001 = so_001.move(x=0, y=0, z=89)
        so_001 = so_001.apply_matrix_rotation([0, 0, 1, 0, 1, 0, -1, 0, 0])
        so_002 = SOFLRFCL150(s, 50, cel_mm=5, add_ports=False)
        so_002 = so_002.apply_matrix_rotation([0, 0, -1, 0, 1, 0, 1, 0, 0])
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.move(x=-0, y=0, z=-89)
        so_002 = so_002.move(x=0, y=-0, z=89)
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.move(x=-0, y=0, z=-89)
        so_002 = so_002.move(x=0, y=-0, z=89)
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.move(x=-0, y=0, z=-89)
        so_002 = so_002.move(x=0, y=-0, z=89)
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.apply_matrix_rotation([1, -0, -0, 0, 0, 1, -0, -1, 0])
        so_002 = so_002.move(x=-0, y=0, z=-89)
        so_002 = so_002.apply_matrix_rotation([0, 0, 1, 0, 1, 0, -1, 0, 0])
        hsph_001 = HalfSphere(s, radius=55)
        hsph_001 = hsph_001.rotateY(90)
        hsph_001 = hsph_001.rotateZ(90)
        hsph_001 = hsph_001.rotateZ(90)
        hsph_001 = hsph_001.move(x=-10, y=0, z=0)
        cyl_001 = cyl_001.combine([cone_001])
        cyl_001 = cyl_001.combine([so_001])
        cyl_001 = cyl_001.combine([so_002])
        cyl_001 = cyl_001.combine([hsph_001])
        cyl_001 = cyl_001.combine([bfil_001])
        cyl_001 = cyl_001.combine([bfil_002])
        geom = cyl_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Auto-generated from Port Manager (scene graph)."""
        # Port 1 (FL)
        prim.set_port(
            s,
            prim.Point3d(89, 0, 0),
            prim.Point3d(1, 0, 0),
        )
        # Port 2 (FL)
        prim.set_port(
            s,
            prim.Point3d(-89, 0, 0),
            prim.Point3d(-1, 0, -0),
        )
        return self
