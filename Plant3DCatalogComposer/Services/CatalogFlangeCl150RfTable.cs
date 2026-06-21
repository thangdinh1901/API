// ASME B16.5 Class 150 RF flange body thickness tf (mm) — mirrors BLD_FLRF / WN python tables.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogFlangeCl150RfTable
    {
        private static readonly Dictionary<int, double> TfByDn = new()
        {
            [15] = 9.7,
            [20] = 11.2,
            [25] = 12.7,
            [32] = 14.2,
            [40] = 15.9,
            [50] = 17.5,
            [65] = 20.6,
            [80] = 22.4,
            [90] = 22.4,
            [100] = 22.4,
            [125] = 22.4,
            [150] = 23.9,
            [200] = 26.9,
            [250] = 28.4,
            [300] = 30.2,
            [350] = 33.3,
            [400] = 35.1,
            [450] = 38.1,
        };

        public static IReadOnlyList<int> AllDns { get; } = TfByDn.Keys.OrderBy(k => k).ToList();

        public static bool TryGetTf(int dn, out double tf) => TfByDn.TryGetValue(dn, out tf);
    }
}
