using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>
    /// Evaluates simple skeleton expressions such as "BodyOD * 0.5".
    /// Parameter names are substituted with current skeleton values before evaluation.
    /// </summary>
    public static class ExpressionEvaluator
    {
        private static readonly string[] ParameterNames =
        {
            "FaceToFace", "BodyOD", "BodyLength", "BonnetHeight", "StemDia", "HandwheelOD", "DN",
        };

        public static bool TryEvaluate(string? expression, SkeletonParameters skeleton, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string trimmed = expression.Trim();

            // Bare parameter name
            if (IsParameterName(trimmed))
            {
                value = skeleton.Resolve(trimmed);
                return true;
            }

            try
            {
                string resolved = SubstituteParameters(trimmed, skeleton);
                object? result = new DataTable().Compute(resolved, null);
                if (result == null)
                    return false;

                value = Convert.ToDouble(result, CultureInfo.InvariantCulture);
                return value > 0;
            }
            catch
            {
                return false;
            }
        }

        public static double EvaluateOrValue(string? expression, SkeletonParameters skeleton, double fallback)
        {
            return TryEvaluate(expression, skeleton, out double v) ? v : fallback;
        }

        private static string SubstituteParameters(string expression, SkeletonParameters skeleton)
        {
            string result = expression;
            foreach (string name in ParameterNames)
            {
                result = Regex.Replace(
                    result,
                    $@"\b{Regex.Escape(name)}\b",
                    skeleton.Resolve(name).ToString(CultureInfo.InvariantCulture),
                    RegexOptions.IgnoreCase);
            }
            return result;
        }

        private static bool IsParameterName(string text) =>
            Array.Exists(ParameterNames, n => string.Equals(n, text, StringComparison.OrdinalIgnoreCase));
    }
}
