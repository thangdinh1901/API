using System;
using System.Collections.Generic;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>Plant 3D Spec Editor — Primary End Type / PLANTENDCODES (Create New Component).</summary>
    public static class Plant3DEndTypes
    {
        public static IReadOnlyList<(string Code, string Description)> All { get; } =
        [
            ("Undefined_ET", "Undefined"),
            ("PL", "Plain"),
            ("BV", "Beveled (butt weld)"),
            ("THDM", "Threaded male"),
            ("THDF", "Threaded female"),
            ("SW", "Socket weld"),
            ("FL", "Flanged"),
            ("WF", "Wafer"),
            ("LAP", "Lap joint"),
            ("GRV", "Grooved"),
            ("SO", "Slip-on"),
            ("PPL", "Preparation plain"),
            ("PSW", "Preparation socket weld"),
            ("LFL", "Lined flanged"),
            ("LLP", "Lined lap joint"),
            ("LUG", "Lug"),
            ("BELL", "Bell"),
            ("SPIG", "Spigot"),
            ("TAP", "Tap"),
            ("MJM", "Mechanical joint (male)"),
            ("MJF", "Mechanical joint (female)"),
            ("MJP", "Mechanical joint (plain)"),
            ("PFS", "Push-fit socket"),
            ("Universal_ET", "Universal"),
            ("TC", "Tri-Clamp"),
            ("C", "Compression"),
            ("FTG", "Flare tubing"),
            ("FA", "Flare adapter"),
            ("P", "Plain end (PE)"),
            ("SL", "Seal"),
        ];

        public static string NormalizeCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Undefined_ET";

            string code = raw.Trim();
            foreach ((string known, _) in All)
            {
                if (known.Equals(code, StringComparison.OrdinalIgnoreCase))
                    return known;
            }

            return code.ToUpperInvariant() switch
            {
                "BW" or "BUTT_WELD" => "BV",
                "FLANGED" => "FL",
                "THREADED" or "TH" => "THDM",
                "SOCKET_WELD" => "SW",
                "UNDEFINED" => "Undefined_ET",
                "UNIVERSAL" => "Universal_ET",
                _ => code.ToUpperInvariant(),
            };
        }

        public static bool IsKnownCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string code = raw.Trim();
            foreach ((string known, _) in All)
            {
                if (known.Equals(code, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string GetDescription(string? code)
        {
            string normalized = NormalizeCode(code);
            foreach ((string known, string description) in All)
            {
                if (known.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    return description;
            }

            return normalized;
        }

        public static string FormatDisplay(string? code) =>
            $"{NormalizeCode(code)} — {GetDescription(code)}";
    }
}
