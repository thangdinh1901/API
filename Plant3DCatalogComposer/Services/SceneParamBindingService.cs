using System;
using System.Globalization;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Manual Scene parameter values win over stale skeleton expressions (Duplicate + edit, etc.).
    /// Keeps JSON, preview (scene_builder), and catalog export aligned.
    /// </summary>
    internal static class SceneParamBindingService
    {
        private const double Tolerance = 1e-9;

        public static void SanitizeManualParameterOverrides(ValveProject project)
        {
            foreach (PrimitiveNode node in project.Parts)
                SanitizeManualParameterOverrides(node, project.Parameters);
        }

        public static void SanitizeManualParameterOverrides(PrimitiveNode node, SkeletonParameters skeleton)
        {
            foreach (ParamValue param in node.Parameters.Values)
            {
                if (TryClearStaleExpression(param, skeleton))
                    param.Expression = null;
            }
        }

        /// <summary>True when stored value intentionally differs from what the expression evaluates to.</summary>
        public static bool HasManualOverride(ParamValue param, SkeletonParameters skeleton)
        {
            string expr = param.Expression?.Trim() ?? "";
            if (string.IsNullOrEmpty(expr))
                return false;

            if (!ExpressionEvaluator.TryEvaluate(expr, skeleton, out double resolved))
                return false;

            if (param.Value <= 0 && resolved > 0)
                return false;

            return Math.Abs(param.Value - resolved) > Tolerance;
        }

        public static bool TryClearStaleExpression(ParamValue param, SkeletonParameters skeleton) =>
            HasManualOverride(param, skeleton);

        public static string FormatCatalogDimensionRef(
            ParamValue? param,
            SkeletonParameters skeleton,
            double fallback)
        {
            if (param == null)
                return Fmt(fallback);

            string? expr = param.Expression?.Trim();
            if (!string.IsNullOrEmpty(expr)
                && IsCatalogDimensionIdentifier(expr, skeleton)
                && !ExpressionEvaluator.IsNumericLiteral(expr))
            {
                if (HasManualOverride(param, skeleton))
                    return Fmt(param.Value);

                return expr;
            }

            return Fmt(param.Value > 0 ? param.Value : fallback);
        }

        public static bool IsCatalogDimensionIdentifier(string name, SkeletonParameters skeleton)
        {
            if (string.IsNullOrWhiteSpace(name)
                || ExpressionEvaluator.IsNumericLiteral(name)
                || name.Length == 0
                || !char.IsLetter(name[0])
                || !name.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }

            if (name.Equals("BendRadius", StringComparison.OrdinalIgnoreCase))
                return true;

            if (ProjectDimensionService.IsBuiltIn(name))
                return !name.Equals("ElbowCenterToFace", StringComparison.OrdinalIgnoreCase);

            return skeleton.CustomDimensions.Keys.Any(k =>
                k.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static string Fmt(double v) =>
            v.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
