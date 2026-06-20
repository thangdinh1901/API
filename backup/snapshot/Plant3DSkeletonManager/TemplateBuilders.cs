using Inventor;

namespace Plant3DSkeletonManager
{
    /// <summary>
    /// Builds the parametric geometry of each primitive template part.
    /// Each builder creates the user parameters that drive the geometry,
    /// mirroring the primitive definitions of the Plant 3D catalog script.
    /// </summary>
    internal static partial class TemplateBuilders
    {
        /// <summary>Cylinder along +Z. Parameters: D (diameter), L (height).</summary>
        public static void Cylinder(Inventor.Application app, PartComponentDefinition def)
        {
            UserParameters up = def.Parameters.UserParameters;
            up.AddByExpression("D", "100 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("L", "150 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("O", "50 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("R2", "50 mm", UnitsTypeEnum.kMillimeterLengthUnits);

            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[3]);

            SketchCircle circle = sk.SketchCircles.AddByCenterRadius(g.CreatePoint2d(0, 0), 5);
            DimensionConstraint dim = (DimensionConstraint)sk.DimensionConstraints.AddDiameter(
                (SketchEntity)circle, g.CreatePoint2d(7, 7));
            dim.Parameter.Expression = "D";

            ExtrudeBy(def, sk, "L");
        }

        /// <summary>
        /// Box centered in X/Y with bottom on the XY plane (primitives.py convention).
        /// Parameters: L (length), W1 (width), H1 (height).
        /// </summary>
        public static void Box(Inventor.Application app, PartComponentDefinition def)
        {
            UserParameters up = def.Parameters.UserParameters;
            up.AddByExpression("L", "150 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("W1", "100 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("H1", "100 mm", UnitsTypeEnum.kMillimeterLengthUnits);

            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[3]);

            SketchEntitiesEnumerator lines = sk.SketchLines.AddAsTwoPointRectangle(
                g.CreatePoint2d(-7.5, -5), g.CreatePoint2d(7.5, 5));
            SketchLine bottom = (SketchLine)lines[1];
            SketchLine right = (SketchLine)lines[2];

            // Center the rectangle on the sketch origin so the box stays centered for any L/W
            SketchPoint center = sk.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            sk.GeometricConstraints.AddGround((SketchEntity)center);
            sk.GeometricConstraints.AddMidpoint(center, bottom);

            DimensionConstraint dimL = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                bottom.StartSketchPoint, bottom.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(0, -7));
            dimL.Parameter.Expression = "L";

            DimensionConstraint dimW = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                right.StartSketchPoint, right.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(10, 0));
            dimW.Parameter.Expression = "W1";

            ExtrudeBy(def, sk, "H1");
        }

        /// <summary>Cone / truncated cone along +Z. Parameters: D1 (bottom), D2 (top), H.</summary>
        public static void Cone(Inventor.Application app, PartComponentDefinition def)
        {
            UserParameters up = def.Parameters.UserParameters;
            up.AddByExpression("D1", "100 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("D2", "50 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("H1", "150 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByValue("E1", 0, UnitsTypeEnum.kUnitlessUnits);

            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]); // XZ plane

            SketchLine bottom = sk.SketchLines.AddByTwoPoints(
                g.CreatePoint2d(0, 0), g.CreatePoint2d(5, 0));
            SketchLine slant = sk.SketchLines.AddByTwoPoints(
                bottom.EndSketchPoint, g.CreatePoint2d(2.5, 15));
            SketchLine top = sk.SketchLines.AddByTwoPoints(
                slant.EndSketchPoint, g.CreatePoint2d(0, 15));
            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                top.EndSketchPoint, bottom.StartSketchPoint);

            sk.GeometricConstraints.AddGround((SketchEntity)bottom.StartSketchPoint);

            DimensionConstraint dimR1 = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                bottom.StartSketchPoint, bottom.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(3, -2));
            dimR1.Parameter.Expression = "D1 / 2";

            DimensionConstraint dimR2 = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                top.StartSketchPoint, top.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(2, 17));
            dimR2.Parameter.Expression = "D2 / 2";

            DimensionConstraint dimH = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                bottom.StartSketchPoint, top.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(-2, 7));
            dimH.Parameter.Expression = "H1";

            RevolveBy(def, sk, axis);
        }

        /// <summary>Torus in XY plane centered at origin. Parameters: D (center-circle diameter), T (tube thickness).</summary>
        public static void Torus(Inventor.Application app, PartComponentDefinition def)
        {
            UserParameters up = def.Parameters.UserParameters;
            up.AddByExpression("D", "160 mm", UnitsTypeEnum.kMillimeterLengthUnits);
            up.AddByExpression("T1", "24 mm", UnitsTypeEnum.kMillimeterLengthUnits);

            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]); // XZ plane

            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                g.CreatePoint2d(0, -3), g.CreatePoint2d(0, 3));
            axis.Centerline = true;
            sk.GeometricConstraints.AddGround((SketchEntity)axis);

            SketchPoint origin = sk.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            sk.GeometricConstraints.AddGround((SketchEntity)origin);

            SketchCircle tube = sk.SketchCircles.AddByCenterRadius(g.CreatePoint2d(8, 0), 1.2);

            DimensionConstraint dimT = (DimensionConstraint)sk.DimensionConstraints.AddDiameter(
                (SketchEntity)tube, g.CreatePoint2d(10, 3));
            dimT.Parameter.Expression = "T1";

            DimensionConstraint dimD = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                origin, tube.CenterSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(4, -3));
            dimD.Parameter.Expression = "D / 2";

            RevolveBy(def, sk, axis);
        }

        /// <summary>
        /// Full sphere whose bottom touches the XY plane, center at Z = R
        /// (primitives.py Sphere convention). Parameter: R (radius).
        /// </summary>
        public static void Sphere(Inventor.Application app, PartComponentDefinition def)
        {
            UserParameters up = def.Parameters.UserParameters;
            up.AddByExpression("R", "50 mm", UnitsTypeEnum.kMillimeterLengthUnits);

            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]); // XZ plane

            SketchPoint basePoint = sk.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            sk.GeometricConstraints.AddGround((SketchEntity)basePoint);

            SketchArc arc = sk.SketchArcs.AddByCenterStartEndPoint(
                g.CreatePoint2d(0, 5), g.CreatePoint2d(0, 0), g.CreatePoint2d(0, 10), true);
            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                arc.EndSketchPoint, arc.StartSketchPoint);

            sk.GeometricConstraints.AddCoincident(
                (SketchEntity)basePoint, (SketchEntity)arc.StartSketchPoint);
            sk.GeometricConstraints.AddVertical((SketchEntity)axis);

            DimensionConstraint dimR = (DimensionConstraint)sk.DimensionConstraints.AddRadius(
                (SketchEntity)arc, g.CreatePoint2d(5, 5));
            dimR.Parameter.Expression = "R";

            RevolveBy(def, sk, axis);
        }

        /// <summary>Hemisphere with base on the XY plane. Parameter: R (radius).</summary>
        public static void HalfSphere(Inventor.Application app, PartComponentDefinition def)
        {
            UserParameters up = def.Parameters.UserParameters;
            up.AddByExpression("R", "50 mm", UnitsTypeEnum.kMillimeterLengthUnits);

            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]); // XZ plane

            SketchArc arc = sk.SketchArcs.AddByCenterStartEndPoint(
                g.CreatePoint2d(0, 0), g.CreatePoint2d(5, 0), g.CreatePoint2d(0, 5), true);
            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                arc.EndSketchPoint, g.CreatePoint2d(0, 0));
            SketchLine bottom = sk.SketchLines.AddByTwoPoints(
                axis.EndSketchPoint, arc.StartSketchPoint);

            sk.GeometricConstraints.AddGround((SketchEntity)axis.EndSketchPoint);

            DimensionConstraint dimR = (DimensionConstraint)sk.DimensionConstraints.AddRadius(
                (SketchEntity)arc, g.CreatePoint2d(5, 5));
            dimR.Parameter.Expression = "R";

            RevolveBy(def, sk, axis);
        }

        private static void ExtrudeBy(PartComponentDefinition def, PlanarSketch sk, string distanceExpression)
        {
            Profile profile = sk.Profiles.AddForSolid();
            ExtrudeDefinition ed = def.Features.ExtrudeFeatures.CreateExtrudeDefinition(
                profile, PartFeatureOperationEnum.kNewBodyOperation);
            ed.SetDistanceExtent(distanceExpression, PartFeatureExtentDirectionEnum.kPositiveExtentDirection);
            def.Features.ExtrudeFeatures.Add(ed);
        }

        private static void RevolveBy(PartComponentDefinition def, PlanarSketch sk, SketchLine axis)
        {
            Profile profile = sk.Profiles.AddForSolid();
            def.Features.RevolveFeatures.AddFull(
                profile, axis, PartFeatureOperationEnum.kNewBodyOperation);
        }
    }
}
