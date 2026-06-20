using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Top-level catalog families in the Setup panel (extensible via part.json category).</summary>
    public static class CatalogCategories
    {
        public const string Flange = "Flange";
        public const string Gasket = "Gasket";
        public const string Buttwelded = "Buttwelded";
        public const string Threaded = "Threaded";
        public const string Socketwelded = "Socketwelded";
        public const string Olet = "Olet";

        public static IReadOnlyList<CatalogCategoryOption> All { get; } =
        [
            new(Flange, "Flange"),
            new(Gasket, "Gasket"),
            new(Buttwelded, "Buttwelded Fittings"),
            new(Threaded, "Threaded Fittings"),
            new(Socketwelded, "Socketwelded Fittings"),
            new(Olet, "Olets"),
        ];
    }

    public sealed class CatalogCategoryOption(string id, string display)
    {
        public string Id { get; } = id;

        public string Display { get; } = display;

        public override string ToString() => Display;
    }
}
