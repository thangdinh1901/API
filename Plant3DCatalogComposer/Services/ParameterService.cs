using System;
using System.Collections.Generic;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public static class ParameterService
    {
        public static readonly string[] DimensionNames =
            { "FaceToFace", "BodyOD", "BodyLength", "BonnetHeight", "StemDia", "HandwheelOD" };

        private static readonly Dictionary<string, double[]> ValveFactors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Gate Valve"] = new[] { 3.6, 1.8, 2.0, 3.0, 0.40, 1.6 },
                ["Globe Valve"] = new[] { 4.0, 1.9, 2.6, 2.4, 0.40, 1.6 },
                ["Ball Valve"] = new[] { 1.6, 1.6, 1.5, 1.2, 0.35, 1.4 },
                ["Butterfly Valve"] = new[] { 0.9, 1.4, 0.6, 1.5, 0.30, 1.2 },
                ["Check Valve"] = new[] { 3.4, 1.7, 2.2, 1.0, 0.30, 1.0 },
            };

        public static IReadOnlyCollection<string> ValveTypes => ValveFactors.Keys;

        public static double[] SuggestDimensions(string valveType, double dn)
        {
            if (!ValveFactors.TryGetValue(valveType, out double[]? f))
                throw new ArgumentException($"Unknown valve type: {valveType}");

            var result = new double[f.Length];
            for (int i = 0; i < f.Length; i++)
                result[i] = dn * f[i];
            return result;
        }

        public static void ApplySkeleton(ValveProject project, string valveType, SkeletonParameters data)
        {
            project.Parameters = data;
            project.CustomPartId = null;
            project.ValveName =
                $"{valveType.Replace(" ", "")}_DN{data.DN:0.###}_{data.PressureClass}";
        }

        public static void ApplyCompositeTemplate(ValveProject project, CustomPartDefinition part, SkeletonParameters data)
        {
            project.Parameters = data;
            project.CustomPartId = part.Id;
            project.ValveName = $"{part.Id}_DN{data.DN:0.###}_{data.PressureClass}";
        }
    }
}
