# Port Manager: 2 connection point(s) — add_ports() below
# P3D_SCENE_MANIFEST: 5 part(s): ELBO_001, CYL_001, CYL_002, FLAN_001, FLAN_002
import primitives as prim
from primitives import (
    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,
    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,
    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,
    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,
    BoxWithFillet, CylinderChamfered, CylinderWithFillet, Fillet,
    ShapeObject,
)
from CUST_ELBOW_90_SCH40_BW import ELBOW_90_SCH40_BW
from CUST_FLANGE_LJ_RF_CL150 import FLANGE_LJ_RF_CL150

class BEND_3D_JACOB(ShapeObject):
    def __init__(self, s, DN=100, FaceToFace=351.9, *, add_ports=True, **_):
        # --- geometry (scene graph) ---
        elbo_001 = ELBOW_90_SCH40_BW(s, 100, add_ports=False)
        cyl_001 = Cylinder(s, diameter=114.3, height=228.6)
        cyl_001 = cyl_001.rotateY(90)
        cyl_001 = cyl_001.move(x=150, y=0, z=0)
        cyl_002 = Cylinder(s, diameter=114.3, height=228.6)
        cyl_002 = cyl_002.rotateY(90)
        cyl_002 = cyl_002.rotateZ(90)
        cyl_002 = cyl_002.move(x=0, y=150, z=0)
        flan_001 = FLANGE_LJ_RF_CL150(s, 100, add_ports=False)
        flan_001 = flan_001.move(x=354.6, y=0, z=-0)
        flan_002 = FLANGE_LJ_RF_CL150(s, 100, add_ports=False)
        flan_002 = flan_002.rotateZ(90)
        flan_002 = flan_002.move(x=0, y=354.6, z=-0)
        elbo_001 = elbo_001.combine([cyl_001])
        elbo_001 = elbo_001.combine([cyl_002])
        elbo_001 = elbo_001.combine([flan_001])
        elbo_001 = elbo_001.combine([flan_002])
        geom = elbo_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Auto-generated from Port Manager (scene graph)."""
        # Port 1 (FL)
        prim.set_port(
            s,
            prim.Point3d(378.5, 0, 0),
            prim.Point3d(1, -0, 0),
        )
        # Port 2 (FL)
        prim.set_port(
            s,
            prim.Point3d(-0, 378.6, 0),
            prim.Point3d(0, 1, 0),
        )
        return self
