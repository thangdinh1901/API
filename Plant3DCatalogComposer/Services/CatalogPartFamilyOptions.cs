using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogPartFamilyOptions
    {
        private static readonly Dictionary<string, string[]> PipingComponentsByCategory =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [CatalogCategories.Fittings] =
                [
                    "Elbow", "Coupling", "Cross", "Crossover", "Lateral", "Nipple",
                    "Reducer", "Swage", "Sleeve", "Tee", "Wye",
                ],
                [CatalogCategories.Miscellaneous] =
                [
                    "BleedRing", "BlindDisk", "Cap", "OrificePlate", "Plug",
                    "SpacerDisk", "SpectacleBlind", "Strainer",
                ],
                [CatalogCategories.Olet] =
                [
                    "Olet", "ElbowSideOutlet", "TeeSideOutlet",
                ],
                [CatalogCategories.Fasteners] =
                [
                    "BoltSet", "Clamp", "Collar", "Gasket", "Gland", "StubEnd",
                    "BackingRing", "ReinforcementRing",
                ],
                [CatalogCategories.Flanges] =
                [
                    "Flange", "BlindFlange",
                ],
                [CatalogCategories.Pipe] =
                [
                    "CS", "SS", "HDPE", "Pipe",
                ],
                [CatalogCategories.Valves] =
                [
                    "Valve", "ValveBody",
                ],
                [CatalogCategories.Actuators] =
                [
                    "ValveActuator",
                ],
                [CatalogCategories.Instruments] =
                [
                    "Instrument",
                ],
            };

        public static IReadOnlyList<string> GetPipingComponents(string? componentCategoryId)
        {
            string category = CatalogCategories.NormalizeCategoryId(componentCategoryId);
            if (PipingComponentsByCategory.TryGetValue(category, out string[]? list))
                return list;

            return PipingComponentsByCategory[CatalogCategories.Fittings];
        }

        public static IReadOnlyList<string> AllPipingComponents { get; } =
            PipingComponentsByCategory.Values.SelectMany(v => v).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>Tee, reducer, and fittings/olet with a second nominal size (DN small).</summary>
        public static bool UsesDnSmall(string? categoryId, string? pipingComponent)
        {
            string category = CatalogCategories.NormalizeCategoryId(categoryId);
            string component = pipingComponent?.Trim() ?? "";
            if (string.IsNullOrEmpty(component))
                return false;

            if (category.Equals(CatalogCategories.Fittings, StringComparison.OrdinalIgnoreCase))
            {
                return component.Equals("Tee", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Reducer", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Cross", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Lateral", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Wye", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Coupling", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Swage", StringComparison.OrdinalIgnoreCase);
            }

            if (category.Equals(CatalogCategories.Olet, StringComparison.OrdinalIgnoreCase))
            {
                return component.Equals("Olet", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("ElbowSideOutlet", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("TeeSideOutlet", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>Plant 3D @activate Group — derived from Component Category + Piping Component.</summary>
        public static string ResolveActivateGroup(string? componentCategoryId, string? pipingComponent)
        {
            string category = CatalogCategories.NormalizeCategoryId(componentCategoryId);
            string pnp = pipingComponent?.Trim() ?? "";

            return category switch
            {
                CatalogCategories.Flanges => "Flange",
                CatalogCategories.Fittings => "Fitting",
                CatalogCategories.Olet => "Olet",
                CatalogCategories.Valves => "Valve",
                CatalogCategories.Pipe => "Pipe",
                CatalogCategories.Instruments => "Instrument",
                CatalogCategories.Fasteners when pnp.Equals("Gasket", StringComparison.OrdinalIgnoreCase) => "Gasket",
                CatalogCategories.Fasteners => "Fitting",
                CatalogCategories.Miscellaneous => "Fitting",
                CatalogCategories.Actuators => "Custom",
                _ => "Custom",
            };
        }
    }

    internal sealed class PrimaryEndTypeOption(string code, string display)
    {
        public string Code { get; } = code;

        public string Display { get; } = display;

        public override string ToString() => Display;
    }
}
