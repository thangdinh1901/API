using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>ASME B16.11 Class 3000 socket-weld reducing-tee DN pairs (NPS 1/2–4).</summary>
    internal static class SwFittingSizeCatalog
    {
        private static readonly (int Large, int Small)[] ReducingTeePairs =
        [
            (20, 15),
            (25, 20), (25, 15),
            (32, 25), (32, 20),
            (40, 32), (40, 25),
            (50, 40), (50, 32), (50, 25),
            (65, 50), (65, 40),
            (80, 65), (80, 50),
            (100, 80), (100, 65), (100, 50),
        ];

        public static bool IsValidReducingTeePair(int largeDn, int smallDn) =>
            ReducingTeePairs.Any(p => p.Large == largeDn && p.Small == smallDn);

        public static IReadOnlyList<int> BranchSizes(int runDn) =>
            ReducingTeePairs
                .Where(p => p.Large == runDn)
                .Select(p => p.Small)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

        public static int DefaultBranchDn(int runDn)
        {
            IReadOnlyList<int> sizes = BranchSizes(runDn);
            if (sizes.Count == 0)
                throw new InvalidOperationException(
                    $"No standard B16.11 SW reducing tee branch for run DN {runDn}.");
            return sizes[0];
        }

        public static int NormalizeBranchDn(int runDn, int branchDn)
        {
            if (IsValidReducingTeePair(runDn, branchDn))
                return branchDn;
            return DefaultBranchDn(runDn);
        }
    }
}
