using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Clone-source Excel sheets for custom valves (CatalogBuilderTemplate.xlsx).</summary>
    internal static class CatalogValveExcelTemplates
    {
        public const string FlSimple = "VALVE_FL_CL150";
        public const string FlRich = "VALVE_FL_RICH";
        public const string BvCl150 = "VALVE_BV_CL150";
        public const string SwCl3000 = "VALVE_SW_CL3000";
        public const string ThreeWay = "VALVE_3WAY";
        public const string Angle = "VALVE_ANGLE";
        public const string Psv = "VALVE_PSV";

        public static IReadOnlyList<string> All { get; } =
        [
            FlSimple,
            FlRich,
            BvCl150,
            SwCl3000,
            ThreeWay,
            Angle,
            Psv,
        ];

        public static bool IsValveTemplate(string? partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return false;

            foreach (string id in All)
            {
                if (partId.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>Match generic valve Excel clone id to Part Family primary end type.</summary>
        public static bool MatchesPrimaryEnd(string templatePartId, string? primaryEndType)
        {
            if (string.IsNullOrWhiteSpace(templatePartId))
                return false;

            string id = templatePartId.ToUpperInvariant();
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);

            if (id.Equals(BvCl150, StringComparison.OrdinalIgnoreCase)
                || id.Equals(Angle, StringComparison.OrdinalIgnoreCase))
            {
                return end.Equals("BV", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("PL", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("BW", StringComparison.OrdinalIgnoreCase);
            }

            if (id.Equals(SwCl3000, StringComparison.OrdinalIgnoreCase))
            {
                return end.Equals("SW", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("THDF", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("THDM", StringComparison.OrdinalIgnoreCase);
            }

            if (id.Equals(FlSimple, StringComparison.OrdinalIgnoreCase)
                || id.Equals(FlRich, StringComparison.OrdinalIgnoreCase)
                || id.Equals(ThreeWay, StringComparison.OrdinalIgnoreCase)
                || id.Equals(Psv, StringComparison.OrdinalIgnoreCase))
            {
                return end.Equals("FL", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("SO", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("WF", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("LUG", StringComparison.OrdinalIgnoreCase)
                    || end.Equals("Undefined_ET", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        /// <summary>Minimum ConnPortNum / Ports metadata when Port Manager is empty.</summary>
        public static int MinimumConnectionPortCount(string? templatePartId)
        {
            if (string.IsNullOrWhiteSpace(templatePartId))
                return 0;

            if (templatePartId.Equals(ThreeWay, StringComparison.OrdinalIgnoreCase))
                return 3;

            return IsValveTemplate(templatePartId) ? 2 : 0;
        }

        /// <summary>FirstPortEndtypes for valve Excel clone templates (no Port Manager).</summary>
        public static string InferFirstPortEndtypes(string templatePartId, int portCount)
        {
            if (portCount <= 0)
                return "FL";

            string portEnd = templatePartId.ToUpperInvariant() switch
            {
                var id when id.Equals(BvCl150, StringComparison.Ordinal)
                    || id.Equals(Angle, StringComparison.Ordinal) => "BV",
                var id when id.Equals(SwCl3000, StringComparison.Ordinal) => "SW",
                _ => "FL",
            };

            return portCount <= 1
                ? portEnd
                : string.Join(",", Enumerable.Repeat(portEnd, portCount));
        }
    }
}
