# Port Manager: no ports — add_ports() uses library defaults or TODO placeholders
import primitives as prim
from primitives import (
    Box, Cone, Cylinder, Elbow, EllipsoidHead, EllipsoidHead2,
    EllipsoidSegment, HalfSphere, Pyramid, Reduced_elbow,
    RoundRectangle, SegmentedElbow, ShapeAssembly, Sphere,
    SphereSegment, TorisPhericHead, TorisPhericHead2, TorisPhericHeadH, Torus,
    ShapeObject,
)

class GATEVALVE_DN50_150(ShapeObject):
    def __init__(self, s, DN=50, *, add_ports=True):
        # --- geometry (scene graph) ---
        cyl_001 = Cylinder(s, diameter=60.3, height=22.6553)
        cyl_001 = cyl_001.rotateY(90)
        ehead_001 = EllipsoidHead(s, diameter=60.3)
        ehead_001 = ehead_001.rotateY(90)
        ehead_001 = ehead_001.rotateY(90)
        ehead_001 = ehead_001.rotateY(90)
        cyl_001 = cyl_001.combine([ehead_001])
        geom = cyl_001
        super().__init__(geom.obj if hasattr(geom, "obj") else geom)
        if add_ports:
            self.add_ports(s)

    def add_ports(self, s):
        """TODO: refine port positions in Port Manager (default axial ports)."""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d(301.63, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self
