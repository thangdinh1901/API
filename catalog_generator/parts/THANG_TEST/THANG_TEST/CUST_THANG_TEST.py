# Port Manager: 2 connection point(s) — add_ports() below
# P3D_SCENE_MANIFEST: 3 part(s): CYL_001, SPH_001, TOR_001
import primitives as prim
from primitives import (
    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,
    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,
    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,
    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,
    ShapeObject,
)

class THANG_TEST(ShapeObject):
    def __init__(self, s, DN=50, BodyOD=60.3, L=200, *, add_ports=True):
        # --- geometry (scene graph) ---
        cyl_001 = Cylinder(s, diameter=60.3, height=100)
        sph_001 = Sphere(s, radius=35)
        sph_001 = sph_001.move(x=0, y=0, z=50)
        tor_001 = Torus(s, diameter=70, thickness=9.045)
        tor_001 = tor_001.move(x=0, y=0, z=50)
        cyl_001 = cyl_001.combine([sph_001])
        cyl_001 = cyl_001.combine([tor_001])
        geom = cyl_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """Auto-generated from Port Manager (scene graph)."""
        # Port 1 (BV)
        prim.set_port(
            s,
            prim.Point3d(0, 0, 100),
            prim.Point3d(0, 0, 1),
        )
        # Port 2 (BV)
        prim.set_port(
            s,
            prim.Point3d(0, 0, 0),
            prim.Point3d(0, 0, -1),
        )
        return self
