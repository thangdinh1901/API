using System;
using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Plant FLANGE LJ catalog content L,D1,D2 (decimal inches) — from native Plant export.
    /// Used for parametric geometry in Catalog Builder (not Iplex/Iplex script dims).
    /// </summary>
    internal static class CatalogLjRingPlantContentTable
    {
        internal sealed record LjContentDims(double L, double D1, double D2);

        // Keyed by decimal NPS inches (matches NominalDiameter when NominalUnit=in).
        private static readonly Dictionary<double, LjContentDims> ByNpsInches = new()
        {
            [0.5] = new(1.25, 4.75, 0.9), // no native row; scaled from 3/4" for DN15
            [0.75] = new(1.38, 5.12, 1.11),
            [1.0] = new(1.62, 5.88, 1.38),
            [1.25] = new(1.62, 6.25, 1.72),
            [1.5] = new(1.75, 7.0, 1.97),
            [2.0] = new(2.25, 8.5, 2.46),
            [2.5] = new(2.5, 9.62, 2.97),
            [3.0] = new(2.88, 10.5, 3.6),
            [3.5] = new(3.0, 11.0, 4.0), // native export has no 3-1/2; approximate
            [4.0] = new(3.56, 12.25, 4.6),
            [5.0] = new(4.12, 14.75, 5.69),
            [6.0] = new(4.69, 15.5, 6.75),
            [8.0] = new(5.62, 19.0, 8.75),
            [10.0] = new(7.0, 23.0, 10.92),
            [12.0] = new(8.62, 26.5, 12.92),
            [14.0] = new(9.5, 29.5, 14.18),
            [16.0] = new(10.25, 32.5, 16.19),
            [18.0] = new(10.88, 36.0, 18.2),
        };

        public static bool TryGet(int dnMm, out LjContentDims dims)
        {
            dims = default!;
            if (!PipeSizeCatalog.TryGetNpsInches(dnMm, out double nps))
                return false;

            return ByNpsInches.TryGetValue(nps, out dims!);
        }
    }
}
