using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>
    /// Evaluates simple skeleton expressions such as "BodyOD * 0.5".
    /// Parameter names are substituted with current skeleton values before evaluation.
    /// </summary>
    public static class ExpressionEvaluator
    {
        public static bool TryEvaluate(string? expression, SkeletonParameters skeleton, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            string trimmed = expression.Trim();

            if (double.TryParse(
                    trimmed,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double literal))
            {
                value = literal;
                return true;
            }

            // Bare parameter name
            if (IsParameterName(trimmed, skeleton))
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
                return true;
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

        public static bool IsNumericLiteral(string? expression) =>
            !string.IsNullOrWhiteSpace(expression)
            && double.TryParse(
                expression.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _);

        private static string SubstituteParameters(string expression, SkeletonParameters skeleton)
        {
            string result = expression;
            foreach (string name in skeleton.ExpressionParameterNames()
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderByDescending(n => n.Length))
            {
                double value;
                try
                {
                    value = skeleton.Resolve(name);
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }

                result = Regex.Replace(
                    result,
                    $@"\b{Regex.Escape(name)}\b",
                    value.ToString(CultureInfo.InvariantCulture),
                    RegexOptions.IgnoreCase);
            }
            return result;
        }

        private static bool IsParameterName(string text, SkeletonParameters skeleton) =>
            skeleton.ExpressionParameterNames()
                .Any(n => string.Equals(n, text, StringComparison.OrdinalIgnoreCase));
    }
}
