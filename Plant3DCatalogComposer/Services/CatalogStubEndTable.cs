using System;
using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>ASME B16.9 Type A lap-joint stub end — long pattern, Sch-40 (mm).</summary>
    internal static class CatalogStubEndTable
    {
        internal sealed record StubEndDims(double L, double B, double D1, double D2);

        private static readonly Dictionary<int, StubEndDims> ByDn = new()
        {
            [15] = new(76.2, 2.77, 34.9, 21.3),
            [20] = new(76.2, 2.87, 42.9, 26.7),
            [25] = new(101.6, 3.38, 50.8, 33.4),
            [32] = new(101.6, 3.56, 63.5, 42.2),
            [40] = new(101.6, 3.68, 73.0, 48.3),
            [50] = new(152.4, 3.91, 92.1, 60.3),
            [65] = new(152.4, 5.16, 104.8, 73.0),
            [80] = new(152.4, 5.49, 127.0, 88.9),
            [90] = new(152.4, 5.74, 139.7, 101.6),
            [100] = new(152.4, 6.02, 157.2, 114.3),
            [125] = new(203.2, 6.55, 185.7, 141.3),
            [150] = new(203.2, 7.11, 215.9, 168.3),
            [200] = new(203.2, 8.18, 269.9, 219.1),
            [250] = new(254.0, 9.27, 323.9, 273.0),
            [300] = new(254.0, 9.53, 381.0, 323.8),
            [350] = new(304.8, 9.53, 412.8, 355.6),
            [400] = new(304.8, 9.53, 469.9, 406.4),
            [450] = new(304.8, 9.53, 533.4, 457.2),
        };

        public static bool TryGet(int dn, out StubEndDims dims) => ByDn.TryGetValue(dn, out dims!);

        public static StubEndDims Get(int dn) =>
            TryGet(dn, out StubEndDims dims)
                ? dims
                : throw new InvalidOperationException($"No B16.9 stub end dimensions for DN {dn}.");
    }
}
