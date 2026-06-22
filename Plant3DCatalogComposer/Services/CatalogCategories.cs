using System;
using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Plant 3D Spec Editor — Create New Component → Component Category.</summary>
    public static class CatalogCategories
    {
        public const string Fittings = "Fittings";
        public const string Miscellaneous = "Miscellaneous";
        public const string Olet = "Olet";
        public const string Fasteners = "Fasteners";
        public const string Flanges = "Flanges";
        public const string Pipe = "Pipe";
        public const string Valves = "Valves";
        public const string Actuators = "Actuators";
        public const string Instruments = "Instruments";

        public static IReadOnlyList<CatalogCategoryOption> All { get; } =
        [
            new(Fittings, "Fittings"),
            new(Miscellaneous, "Miscellaneous"),
            new(Olet, "Olet"),
            new(Fasteners, "Fasteners"),
            new(Flanges, "Flanges"),
            new(Pipe, "Pipe"),
            new(Valves, "Valves"),
            new(Actuators, "Actuators"),
            new(Instruments, "Instruments"),
        ];

        /// <summary>Maps legacy part.json / project values to Component Category ids.</summary>
        public static string NormalizeCategoryId(string? categoryOrLegacy)
        {
            if (string.IsNullOrWhiteSpace(categoryOrLegacy))
                return Fittings;

            string raw = categoryOrLegacy.Trim();
            foreach (CatalogCategoryOption opt in All)
            {
                if (opt.Id.Equals(raw, StringComparison.OrdinalIgnoreCase))
                    return opt.Id;
            }

            return raw switch
            {
                "Flange" => Flanges,
                "Gasket" => Fasteners,
                "Buttwelded" or "Threaded" or "Socketwelded" => Fittings,
                _ => Fittings,
            };
        }

        public static bool CategoriesMatch(string? storedCategory, string filterCategoryId) =>
            NormalizeCategoryId(storedCategory)
                .Equals(NormalizeCategoryId(filterCategoryId), StringComparison.OrdinalIgnoreCase);

        /// <summary>When only legacy @activate Group is stored on the project.</summary>
        public static string FromActivateGroup(string? group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return Fittings;

            return group.Trim() switch
            {
                "Flange" => Flanges,
                "Gasket" => Fasteners,
                "Olet" => Olet,
                "Valve" => Valves,
                "Pipe" => Pipe,
                "Instrument" => Instruments,
                "Fitting" => Fittings,
                _ => Fittings,
            };
        }
    }

    public sealed class CatalogCategoryOption(string id, string display)
    {
        public string Id { get; } = id;

        public string Display { get; } = display;

        public override string ToString() => Display;
    }
}
