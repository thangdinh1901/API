using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>ASME B16.9 reducer / reducing-tee DN pairs (large, small).</summary>
    internal static class BwFittingSizeCatalog
    {
        private static readonly (int Large, int Small)[] ReducerPairs =
        [
            (20, 15),
            (25, 20), (25, 15),
            (32, 25), (32, 20), (32, 15),
            (40, 32), (40, 25), (40, 20), (40, 15),
            (50, 40), (50, 32), (50, 25), (50, 20),
            (65, 50), (65, 40), (65, 32), (65, 25),
            (80, 65), (80, 50), (80, 40), (80, 32),
            (90, 80), (90, 65), (90, 50), (90, 40), (90, 32),
            (100, 90), (100, 80), (100, 65), (100, 50), (100, 40),
            (125, 100), (125, 90), (125, 80), (125, 65), (125, 50),
            (150, 125), (150, 100), (150, 90), (150, 80), (150, 65),
            (200, 150), (200, 125), (200, 100), (200, 90),
            (250, 200), (250, 150), (250, 125), (250, 100),
            (300, 250), (300, 200), (300, 150), (300, 125),
            (350, 300), (350, 250), (350, 200), (350, 150),
            (400, 350), (400, 300), (400, 250), (400, 200),
            (450, 400), (450, 350), (450, 300), (450, 250),
        ];

        public static bool IsValidReducerPair(int largeDn, int smallDn) =>
            ReducerPairs.Any(p => p.Large == largeDn && p.Small == smallDn);

        public static IReadOnlyList<int> ReducerSmallSizes(int largeDn) =>
            ReducerPairs
                .Where(p => p.Large == largeDn)
                .Select(p => p.Small)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

        public static int DefaultReducerSmallDn(int largeDn)
        {
            IReadOnlyList<int> sizes = ReducerSmallSizes(largeDn);
            if (sizes.Count == 0)
                throw new InvalidOperationException(
                    $"No standard B16.9 reducer small DN for large DN {largeDn}.");
            return sizes[0];
        }

        public static int NormalizeReducerSmallDn(int largeDn, int smallDn)
        {
            if (IsValidReducerPair(largeDn, smallDn))
                return smallDn;
            return DefaultReducerSmallDn(largeDn);
        }
    }
}
