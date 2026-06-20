using System;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>World-space translation and rotation for scene graph nodes.</summary>
    public static class TransformMath
    {
        public static void TranslateWorld(PrimitiveNode node, double dxMm, double dyMm, double dzMm)
        {
            node.Origin[0] += dxMm;
            node.Origin[1] += dyMm;
            node.Origin[2] += dzMm;
        }

        /// <summary>
        /// World jog: orbit Origin about WCS (0,0,0) and post-multiply R.
        /// Preview replays WCS rotate at build origin, then move (Plant3D pivots at 0,0,0).
        /// </summary>
        public static void RotateWorld(PrimitiveNode node, char axis, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            double[] delta = AxisRotationMatrix(axis, rad);
            RotateOriginByMatrix(node, delta);

            double[]? cf = node.CatalogFrameRotation;
            if (cf != null && cf.Length >= 9 && !IsIdentityRotation(cf))
            {
                double[] bodyDelta = ConjugateRotationToBuildFrame(cf, axis, degrees);
                node.Rotation = Multiply3x3(node.Rotation, bodyDelta);
                SyncDirectionFromRotation(node);
            }
            else
            {
                RotateLocal(node, axis, degrees);
            }
        }

        /// <summary>
        /// Spins around the part's current local +X/+Y/+Z through its Origin; Origin unchanged.
        /// </summary>
        public static void RotateLocal(PrimitiveNode node, char axis, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            double[] delta = AxisRotationMatrix(axis, rad);
            node.Rotation = Multiply3x3(node.Rotation, delta);
            SyncDirectionFromRotation(node);
        }

        /// <summary>
        /// Rotates around connection/part axis (post-multiply in catalog frame).
        /// </summary>
        public static void RotateLocalInBodyFrame(
            PrimitiveNode node, char axis, double degrees, double[] catalogFrame)
        {
            if (catalogFrame == null || catalogFrame.Length < 9 || IsIdentityRotation(catalogFrame))
            {
                RotateLocal(node, axis, degrees);
                return;
            }

            double[] bodyDelta = ConjugateRotationToBuildFrame(catalogFrame, axis, degrees);
            node.Rotation = Multiply3x3(node.Rotation, bodyDelta);
            SyncDirectionFromRotation(node);
        }

        private static void RotateOriginByMatrix(PrimitiveNode node, double[] m)
        {
            double x = node.Origin[0], y = node.Origin[1], z = node.Origin[2];
            node.Origin[0] = m[0] * x + m[1] * y + m[2] * z;
            node.Origin[1] = m[3] * x + m[4] * y + m[5] * z;
            node.Origin[2] = m[6] * x + m[7] * y + m[8] * z;
        }

        public static double[] FrameRotationFromAxisDegrees(char axis, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            return AxisRotationMatrix(axis, rad);
        }

        /// <summary>Map WCS or connection-axis delta into catalog build frame: inv(R_cat) * delta * R_cat.</summary>
        public static double[] ConjugateRotationToBuildFrame(double[] catalogFrame, char axis, double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            double[] delta = AxisRotationMatrix(axis, rad);
            if (catalogFrame == null || catalogFrame.Length < 9 || IsIdentityRotation(catalogFrame))
                return delta;

            double[] invCf = InverseRotation3x3(catalogFrame);
            return Multiply3x3(Multiply3x3(invCf, delta), catalogFrame);
        }

        public static double[] InvertRotation(double[] rotation) => InverseRotation3x3(rotation);

        public static void MapWorldVectorToBuildFrame(
            double[]? catalogFrame, double wx, double wy, double wz,
            out double bx, out double by, out double bz)
        {
            if (catalogFrame == null || catalogFrame.Length < 9 || IsIdentityRotation(catalogFrame))
            {
                bx = wx;
                by = wy;
                bz = wz;
                return;
            }

            double[] inv = InverseRotation3x3(catalogFrame);
            bx = inv[0] * wx + inv[1] * wy + inv[2] * wz;
            by = inv[3] * wx + inv[4] * wy + inv[5] * wz;
            bz = inv[6] * wx + inv[7] * wy + inv[8] * wz;
        }

        public static bool IsIdentityRotation(double[]? rotation)
        {
            if (rotation == null || rotation.Length < 9)
                return true;

            const double e = 1e-6;
            return Math.Abs(rotation[0] - 1) < e && Math.Abs(rotation[4] - 1) < e && Math.Abs(rotation[8] - 1) < e
                && Math.Abs(rotation[1]) < e && Math.Abs(rotation[2]) < e && Math.Abs(rotation[3]) < e
                && Math.Abs(rotation[5]) < e && Math.Abs(rotation[6]) < e && Math.Abs(rotation[7]) < e;
        }

        /// <summary>True when origin or rotation differ from default (needs scene_builder transforms).</summary>
        public static bool HasNonDefaultTransform(PrimitiveNode node)
        {
            const double e = 1e-9;
            if (Math.Abs(node.Origin[0]) > e || Math.Abs(node.Origin[1]) > e || Math.Abs(node.Origin[2]) > e)
                return true;

            return !IsIdentityRotation(node.Rotation);
        }

        private static void SyncDirectionFromRotation(PrimitiveNode node)
        {
            double[] r = node.Rotation;
            double x = r[2];
            double y = r[5];
            double z = r[8];
            double len = Math.Sqrt(x * x + y * y + z * z);
            if (len > 1e-12)
            {
                node.Direction = new[] { x / len, y / len, z / len };
            }
        }

        private static double[] AxisRotationMatrix(char axis, double rad)
        {
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);

            return axis switch
            {
                'X' or 'x' => new[] { 1, 0, 0, 0, c, -s, 0, s, c },
                'Y' or 'y' => new[] { c, 0, s, 0, 1, 0, -s, 0, c },
                'Z' or 'z' => new[] { c, -s, 0, s, c, 0, 0, 0, 1 },
                _ => throw new ArgumentException($"Unknown axis '{axis}'."),
            };
        }

        private static double[] Multiply3x3(double[] a, double[] b)
        {
            var c = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++)
                        sum += a[i * 3 + k] * b[k * 3 + j];
                    c[i * 3 + j] = sum;
                }
            }
            return c;
        }

        private static double[] InverseRotation3x3(double[] m) =>
            new[]
            {
                m[0], m[3], m[6],
                m[1], m[4], m[7],
                m[2], m[5], m[8],
            };
    }
}
