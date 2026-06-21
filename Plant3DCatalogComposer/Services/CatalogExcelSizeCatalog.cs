using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogExcelSizeVariant
    {
        public int Dn { get; init; }

        public int? Dn2 { get; init; }

        public double? Cel { get; init; }

        public double? T { get; init; }
    }

    internal static class CatalogExcelSizeCatalog
    {
        private static readonly int[] SwNominalDns =
        [
            6, 8, 10, 15, 20, 25, 32, 40, 50, 65, 80, 100,
        ];

        public static IReadOnlyList<CatalogExcelSizeVariant> BuildSizes(CustomPartDefinition part)
        {
            if (IsReducingPart(part.Id))
                return BuildReducingSizes(part);

            if (part.Id.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
            {
                return CatalogLjRingCl150Table.AllDns
                    .Select(dn => new CatalogExcelSizeVariant { Dn = dn })
                    .ToList();
            }

            if (part.Id.StartsWith("STUBEND_", StringComparison.OrdinalIgnoreCase)
                || part.Id.StartsWith("COLLAR_LJ_", StringComparison.OrdinalIgnoreCase))
            {
                var allowed = ResolveNominalDns(part).ToHashSet();
                return CatalogStubEndTable.AllDns
                    .Where(allowed.Contains)
                    .Select(dn => new CatalogExcelSizeVariant { Dn = dn })
                    .ToList();
            }

            IReadOnlyList<int> dns = ResolveNominalDns(part);
            if (part.Id.StartsWith("SO_", StringComparison.OrdinalIgnoreCase))
            {
                return dns.Select(dn => new CatalogExcelSizeVariant
                {
                    Dn = dn,
                    Cel = CatalogSoFlangeCelTable.TryGetCelMm(dn),
                }).ToList();
            }

            if (part.Id.StartsWith("GSK_", StringComparison.OrdinalIgnoreCase))
            {
                double t = part.CatalogParams.FirstOrDefault(p =>
                    p.Name.Equals("T", StringComparison.OrdinalIgnoreCase))?.Default ?? 1.5;
                return dns.Select(dn => new CatalogExcelSizeVariant { Dn = dn, T = t }).ToList();
            }

            return dns.Select(dn => new CatalogExcelSizeVariant { Dn = dn }).ToList();
        }

        private static bool IsReducingPart(string partId) =>
            partId.Contains("REDUCER", StringComparison.OrdinalIgnoreCase)
            || partId.Contains("TEE_REDUCE", StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<CatalogExcelSizeVariant> BuildReducingSizes(CustomPartDefinition part)
        {
            var list = new List<CatalogExcelSizeVariant>();
            HashSet<int> allowed = ResolveNominalDns(part).ToHashSet();

            foreach ((int large, int small) in EnumerateReducerPairs(part))
            {
                if (!allowed.Contains(large) || !allowed.Contains(small))
                    continue;

                list.Add(new CatalogExcelSizeVariant { Dn = large, Dn2 = small });
            }

            return list
                .OrderBy(v => v.Dn)
                .ThenByDescending(v => v.Dn2 ?? 0)
                .ToList();
        }

        private static IEnumerable<(int Large, int Small)> EnumerateReducerPairs(CustomPartDefinition part)
        {
            if (part.StandardSet.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || part.Id.Contains("_SW_", StringComparison.OrdinalIgnoreCase))
            {
                foreach (int large in SwNominalDns)
                {
                    foreach (int small in BwFittingSizeCatalog.ReducerSmallSizes(large))
                    {
                        if (SwNominalDns.Contains(small))
                            yield return (large, small);
                    }
                }

                yield break;
            }

            foreach (int large in PipeSizeCatalog.NominalSizes.Select(s => s.DnMm))
            {
                foreach (int small in BwFittingSizeCatalog.ReducerSmallSizes(large))
                    yield return (large, small);
            }
        }

        private static IReadOnlyList<int> ResolveNominalDns(CustomPartDefinition part)
        {
            if (part.StandardSet.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || part.Id.Contains("_SW_", StringComparison.OrdinalIgnoreCase))
                return SwNominalDns;

            return PipeSizeCatalog.NominalSizes.Select(s => s.DnMm).ToList();
        }
    }
}
