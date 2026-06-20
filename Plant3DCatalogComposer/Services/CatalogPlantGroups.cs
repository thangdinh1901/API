using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Plant 3D @activate Group values (Spec Editor catalog family).</summary>
    public static class CatalogPlantGroups
    {
        public static IReadOnlyList<string> All { get; } =
        [
            "Flange",
            "Fitting",
            "Olet",
            "Gasket",
            "Valve",
            "Pipe",
            "Instrument",
            "Custom",
        ];
    }
}
