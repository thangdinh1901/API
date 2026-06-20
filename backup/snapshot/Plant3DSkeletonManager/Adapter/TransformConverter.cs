using System;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>
    /// Owns ALL unit and axis conversion between Inventor (cm, Matrix)
    /// and the scene graph (mm, origin/direction/rotation).
    /// </summary>
    public static class TransformConverter
    {
        private const double CmToMm = 10.0;
        private const double MmToCm = 0.1;

        /// <summary>Reads an occurrence transform into the node (world space, mm).</summary>
        public static void ReadTransform(Matrix m, PrimitiveNode node)
        {
            node.Origin = new[]
            {
                m.Cell[1, 4] * CmToMm,
                m.Cell[2, 4] * CmToMm,
                m.Cell[3, 4] * CmToMm,
            };

            node.Rotation = new[]
            {
                m.Cell[1, 1], m.Cell[1, 2], m.Cell[1, 3],
                m.Cell[2, 1], m.Cell[2, 2], m.Cell[2, 3],
                m.Cell[3, 1], m.Cell[3, 2], m.Cell[3, 3],
            };

            // Primitive +Z axis in world coordinates (third column)
            node.Direction = new[]
            {
                m.Cell[1, 3],
                m.Cell[2, 3],
                m.Cell[3, 3],
            };
        }

        /// <summary>Builds an Inventor matrix from a scene graph node (mm → cm).</summary>
        public static Matrix CreateMatrix(TransientGeometry tg, PrimitiveNode node)
        {
            Matrix m = tg.CreateMatrix();
            double[] r = node.Rotation;
            if (r.Length != 9)
                throw new ArgumentException("Rotation matrix must have 9 elements.");

            for (int row = 1; row <= 3; row++)
            {
                for (int col = 1; col <= 3; col++)
                    m.Cell[row, col] = r[(row - 1) * 3 + (col - 1)];
            }

            m.Cell[1, 4] = node.Origin[0] * MmToCm;
            m.Cell[2, 4] = node.Origin[1] * MmToCm;
            m.Cell[3, 4] = node.Origin[2] * MmToCm;
            return m;
        }

        public static double CmValueToMm(double cm) => cm * CmToMm;

        public static string MmExpression(double mm) =>
            mm.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " mm";

        public static string ParamExpression(double value, Core.CatalogParamUnit unit) =>
            unit switch
            {
                Core.CatalogParamUnit.Degree =>
                    value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " deg",
                Core.CatalogParamUnit.Unitless =>
                    value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                _ => MmExpression(value),
            };

        public static double ReadParamValue(Parameter param, Core.CatalogParamUnit unit)
        {
            double raw = (double)param.Value;
            return unit switch
            {
                Core.CatalogParamUnit.Degree => raw * (180.0 / Math.PI),
                Core.CatalogParamUnit.Unitless => raw,
                _ => CmValueToMm(raw),
            };
        }
    }
}
