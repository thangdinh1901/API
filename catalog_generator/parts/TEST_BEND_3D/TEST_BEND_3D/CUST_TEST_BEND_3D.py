# Port Manager: 2 connection point(s) — add_ports() below
# P3D_SCENE_MANIFEST: 7 part(s): CYL_001, ELBO_001, ELBO_002, CONE_001, CONE_002, CONE_003, CONE_004
import primitives as prim
from primitives import (
    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,
    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,
    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,
    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,
    ShapeObject,
)
from CUST_ELBOW_45_SW_CL3000 import ELBOW45SWCL3000

class TEST_BEND_3D(ShapeObject):
    def __init__(self, s, DN=50, BodyOD=60.3, ElbowCenterToFace=220, *, add_ports=True):
        # --- geometry (scene graph) ---
        cyl_001 = Cylinder(s, diameter=60.3, height=200)
        cyl_001 = cyl_001.rotateY(90)
        cyl_001 = cyl_001.move(x=-100, y=0, z=0)
        elbo_001 = ELBOW45SWCL3000(s, 50, add_ports=False)
        elbo_001 = elbo_001.move(x=-130, y=0, z=0)
        elbo_002 = ELBOW45SWCL3000(s, 50, add_ports=False)
        elbo_002 = elbo_002.rotateZ(90)
        elbo_002 = elbo_002.rotateZ(90)
        elbo_002 = elbo_002.move(x=130, y=-0, z=0)
        elbo_002 = elbo_002.move(x=-130, y=0, z=-0)
        elbo_002 = elbo_002.rotateX(90)
        elbo_002 = elbo_002.move(x=130, y=-0, z=0)
        cone_001 = Cone(s, bottom_diameter=60.3, height=50, top_diameter=30.15, eccentricity=0)
        cone_002 = Cone(s, bottom_diameter=60.3, height=50, top_diameter=30.15, eccentricity=0)
        cone_002 = cone_002.rotateX(90)
        cone_002 = cone_002.rotateX(90)
        cone_003 = Cone(s, bottom_diameter=60.3, height=50, top_diameter=30.15, eccentricity=0)
        cone_003 = cone_003.rotateX(90)
        cone_003 = cone_003.rotateX(90)
        cone_003 = cone_003.rotateX(90)
        cone_004 = Cone(s, bottom_diameter=60.3, height=50, top_diameter=30.15, eccentricity=0)
        cone_004 = cone_004.rotateX(90)
        cone_004 = cone_004.rotateX(90)
        cone_004 = cone_004.rotateX(90)
        cone_004 = cone_004.rotateX(90)
        cone_004 = cone_004.rotateX(90)
        cyl_001 = cyl_001.combine([elbo_001])
        cyl_001 = cyl_001.combine([elbo_002])
        cyl_001 = cyl_001.combine([cone_001])
        cyl_001 = cyl_001.combine([cone_002])
        cyl_001 = cyl_001.combine([cone_003])
        cyl_001 = cyl_001.combine([cone_004])
        geom = cyl_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Auto-generated from Port Manager (scene graph)."""
        # Port 1 (SW)
        prim.set_port(
            s,
            prim.Point3d(-148.031, 18.031, 0),
            prim.Point3d(-0.707107, 0.707107, -0),
        )
        # Port 2 (FL)
        prim.set_port(
            s,
            prim.Point3d(148.031, 0, -18.031),
            prim.Point3d(0.707107, -0, -0.707107),
        )
        return self
