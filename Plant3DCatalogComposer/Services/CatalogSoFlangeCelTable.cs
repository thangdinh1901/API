using System;
using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>CEL (mm) per DN for SO_FLRF_CL150 — hub top minus B31.1 pipe setback.</summary>
    internal static class CatalogSoFlangeCelTable
    {
        private static readonly Dictionary<int, double> CelByDn = new()
        {
            [15] = 10.27,
            [20] = 10.09,
            [25] = 11.45,
            [32] = 14.15,
            [40] = 15.92,
            [50] = 20.09,
            [65] = 22.22,
            [80] = 23.51,
            [90] = 24.26,
            [100] = 26.00,
            [125] = 29.00,
            [150] = 32.00,
            [200] = 37.00,
            [250] = 42.00,
            [300] = 48.00,
            [350] = 50.00,
            [400] = 56.00,
            [450] = 61.00,
        };

        public static double TryGetCelMm(int dn)
        {
            if (CelByDn.TryGetValue(dn, out double cel))
                return cel;

            throw new InvalidOperationException($"No CEL table entry for SO flange DN {dn}.");
        }
    }
}
