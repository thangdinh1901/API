using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// When a real catalog workbook (e.g. CATA_NUI.xlsx) is configured as the standard template,
    /// the "Standard Library Parts" insert list is driven by that catalog's sheets. Each sheet is
    /// mapped to the closest local geometry part (active library + ARCHIVED reference parts) by
    /// component / subtype / end-class so it can be previewed in the Composer scene — including
    /// sheets that use native Plant 3D shapes (CPx), which have no geometry of their own.
    /// </summary>
    internal static class CatalogTemplateInsertMapper
    {
        public static bool IsActive => CatalogTemplateSettings.ResolveConfiguredTemplatePath() != null;

        public static IReadOnlyList<CustomPartDefinition> BuildInsertParts()
        {
            IReadOnlyList<TemplatePartInfo> infos = CatalogTemplatePartReader.Read();
            if (infos.Count == 0)
                return Array.Empty<CustomPartDefinition>();

            var pool = CustomPartCatalog.InsertableParts
                .ToList();
            if (pool.Count == 0)
                return Array.Empty<CustomPartDefinition>();

            var result = new List<CustomPartDefinition>();
            foreach (TemplatePartInfo info in infos)
            {
                CustomPartDefinition? geometry = FindBestGeometry(info, pool);
                if (geometry != null)
                    result.Add(geometry.WithDisplayName(info.DisplayName));
            }

            return result
                .OrderBy(p => p.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static CustomPartDefinition? FindBestGeometry(
            TemplatePartInfo info,
            IReadOnlyList<CustomPartDefinition> pool)
        {
            Features want = ExtractFromTemplate(info);
            if (want.Component == Component.Other)
                return null;

            CustomPartDefinition? best = null;
            int bestScore = int.MinValue;

            foreach (CustomPartDefinition candidate in pool)
            {
                int score = Score(want, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            // Require at least a component-level match.
            return bestScore >= 100 ? best : null;
        }

        private static int Score(Features want, CustomPartDefinition candidate)
        {
            Features have = ExtractFromText(
                candidate.Id + " " + candidate.Category + " " + candidate.PressureClass);
            if (have.Component != want.Component)
                return -1;

            int score = 100;

            if (want.Angle != 0 && have.Angle != 0)
                score += want.Angle == have.Angle ? 50 : -30;

            if (want.Component == Component.Reducer || want.Component == Component.Tee)
            {
                if (want.ConcEcc != Variant.None && have.ConcEcc != Variant.None)
                    score += want.ConcEcc == have.ConcEcc ? 40 : -20;
                if (want.EqRed != Variant.None && have.EqRed != Variant.None)
                    score += want.EqRed == have.EqRed ? 40 : -20;
            }

            if (want.Component == Component.Flange && want.FlangeKind != FlangeKind.None
                && have.FlangeKind != FlangeKind.None)
            {
                score += want.FlangeKind == have.FlangeKind ? 40 : 0;
            }

            if ((want.Component == Component.Flange || want.Component == Component.Gasket
                    || want.Component == Component.Blind)
                && want.Facing.Length > 0 && have.Facing.Length > 0)
            {
                score += want.Facing == have.Facing ? 15 : -5;
            }

            bool fitting = want.Component is Component.Elbow or Component.Tee or Component.Reducer;
            if (fitting && want.EndFamily != EndFamily.None && have.EndFamily != EndFamily.None)
                score += want.EndFamily == have.EndFamily ? 30 : -25;

            if (want.PressureClass.Length > 0 && have.PressureClass.Length > 0)
                score += want.PressureClass == have.PressureClass ? 20 : -10;

            return score;
        }

        private enum Component { Other, Elbow, Tee, Reducer, Flange, Blind, Gasket, StubEnd, Pipe, Stud }

        private enum Variant { None, A, B }

        private enum FlangeKind { None, Wn, So, Sw }

        private enum EndFamily { None, ButtWeld, SocketWeld }

        private readonly struct Features
        {
            public Component Component { get; init; }
            public int Angle { get; init; }
            public Variant ConcEcc { get; init; }
            public Variant EqRed { get; init; }
            public FlangeKind FlangeKind { get; init; }
            public EndFamily EndFamily { get; init; }
            public string PressureClass { get; init; }
            public string Facing { get; init; }
        }

        /// <summary>Features for a local geometry candidate, inferred from its id/category/class tokens.</summary>
        private static Features ExtractFromText(string raw)
        {
            string u = (raw ?? "").ToUpperInvariant();

            return new Features
            {
                Component = DetectComponent(u),
                Angle = AngleFrom(u),
                ConcEcc = u.Contains("CONC") ? Variant.A : u.Contains("ECC") ? Variant.B : Variant.None,
                EqRed = (u.Contains("EQUAL") || Has(u, "EQ")) ? Variant.A
                    : (u.Contains("REDUCE") || Has(u, "RED")) ? Variant.B : Variant.None,
                FlangeKind = DetectFlangeKind(u),
                EndFamily = DetectEndFamily(u),
                PressureClass = DetectClass(u),
                Facing = DetectFacing(u),
            };
        }

        /// <summary>
        /// Features for a catalog sheet, read from its data cells (PnPClassName / EndType / Schedule /
        /// PressureClass) plus descriptive text (sheet name + Long Description + ShapeName).
        /// </summary>
        private static Features ExtractFromTemplate(TemplatePartInfo info)
        {
            string text = (info.SheetName + " " + info.DisplayName + " " + info.ShapeName).ToUpperInvariant();

            return new Features
            {
                Component = MapComponent(info.Component, text),
                Angle = AngleFrom(text),
                ConcEcc = text.Contains("CONC") ? Variant.A : text.Contains("ECC") ? Variant.B : Variant.None,
                EqRed = (text.Contains("EQUAL") || Has(text, "EQ")) ? Variant.A
                    : (text.Contains("REDUCE") || Has(text, "RED")) ? Variant.B : Variant.None,
                FlangeKind = DetectFlangeKind(text),
                EndFamily = EndFamilyFromEndType(info.EndType, text),
                PressureClass = ClassFromCells(info.PressureClass, info.Schedule),
                Facing = DetectFacing(text),
            };
        }

        private static int AngleFrom(string u) =>
            u.Contains("180") ? 180 : u.Contains("90") ? 90 : u.Contains("45") ? 45 : 0;

        private static Component MapComponent(string pnpClassName, string text)
        {
            if (text.Contains("BLIND") || Has(text, "BLD"))
                return Component.Blind;

            string p = (pnpClassName ?? "").ToUpperInvariant();
            if (p.Contains("ELBOW")) return Component.Elbow;
            if (p.Contains("TEE")) return Component.Tee;
            if (p.Contains("REDUCER")) return Component.Reducer;
            if (p.Contains("GASKET")) return Component.Gasket;
            if (p.Contains("STUBEND") || p.Contains("STUB")) return Component.StubEnd;
            if (p.Contains("FLANGE")) return Component.Flange;
            if (p.Contains("PIPE")) return Component.Pipe;

            return DetectComponent(text);
        }

        private static EndFamily EndFamilyFromEndType(string endType, string text)
        {
            string e = (endType ?? "").Trim().ToUpperInvariant();
            if (e is "SW" or "PSW")
                return EndFamily.SocketWeld;
            if (e is "BV" or "BW" or "PL" or "PPL")
                return EndFamily.ButtWeld;

            return DetectEndFamily(text);
        }

        private static string ClassFromCells(string pressureClass, string schedule)
        {
            string pc = (pressureClass ?? "").Trim();
            if (pc.Length > 0)
            {
                if (pc.Contains("3000")) return "3000";
                if (pc.Contains("300")) return "300";
                return "150";
            }

            // No pressure class but a schedule (e.g. SCH40) => buttweld/CL150 family for matching.
            return string.IsNullOrWhiteSpace(schedule) ? "" : "150";
        }

        private static string DetectFacing(string u)
        {
            if (Has(u, "RF") || u.Contains("FLRF")) return "RF";
            if (Has(u, "FF") || u.Contains("FLFF")) return "FF";
            return "";
        }

        private static Component DetectComponent(string u)
        {
            if (u.Contains("ELBOW")) return Component.Elbow;
            if (u.Contains("TEE")) return Component.Tee;
            if (u.Contains("REDUCER")) return Component.Reducer;
            if (u.Contains("STUBEND")) return Component.StubEnd;
            if (u.Contains("GSK") || u.Contains("GASKET")) return Component.Gasket;
            if (u.Contains("STUD")) return Component.Stud;
            if (u.Contains("PIPE")) return Component.Pipe;
            if (u.Contains("BLIND") || Has(u, "BLD")) return Component.Blind;
            if (u.Contains("FLANGE") || u.Contains("FLRF") || u.Contains("FLFF")
                || Has(u, "WN") || Has(u, "SO"))
            {
                return Component.Flange;
            }

            return Component.Other;
        }

        private static FlangeKind DetectFlangeKind(string u)
        {
            if (Has(u, "WN")) return FlangeKind.Wn;
            if (Has(u, "SO")) return FlangeKind.So;
            if (Has(u, "SW")) return FlangeKind.Sw;
            return FlangeKind.None;
        }

        private static EndFamily DetectEndFamily(string u)
        {
            if (Has(u, "SW") || u.Contains("3000") || u.Contains("SOCKET"))
                return EndFamily.SocketWeld;
            if (Has(u, "BV") || Has(u, "BW") || u.Contains("SCH") || u.Contains("BUTT") || Has(u, "STD"))
                return EndFamily.ButtWeld;
            return EndFamily.None;
        }

        private static string DetectClass(string u)
        {
            if (u.Contains("3000") || u.Contains("CL3000")) return "3000";
            if (u.Contains("CL300")) return "300";
            if (u.Contains("CL150") || u.Contains("SCH") || Has(u, "BV") || Has(u, "STD")) return "150";
            return "";
        }

        /// <summary>Whole-token match so "SO" does not match "SOCKET" and "WN" does not match "DOWN".</summary>
        private static bool Has(string haystack, string token)
        {
            int idx = 0;
            while ((idx = haystack.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
            {
                bool leftOk = idx == 0 || !char.IsLetterOrDigit(haystack[idx - 1]);
                int end = idx + token.Length;
                bool rightOk = end >= haystack.Length || !char.IsLetterOrDigit(haystack[end]);
                if (leftOk && rightOk)
                    return true;
                idx = end;
            }

            return false;
        }
    }
}
