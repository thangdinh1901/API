using System;
using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class FlangeBoltingSpec
    {
        public required string BoltSize { get; init; }

        public int NumberInSet { get; init; }

        public double LengthMm { get; init; }

        public double LengthInches => Math.Round(LengthMm / 25.4, 6, MidpointRounding.AwayFromZero);
    }

    /// <summary>ASME B16.5 Class 150 RF stud bolting — mirrors STUD_BOLTS/bolting_data.py.</summary>
    internal static class CatalogFlangeBoltingCatalog
    {
        private static readonly Dictionary<int, FlangeBoltingSpec> Class150RfByDn = new()
        {
            [15] = Spec("1/2", 4, 55),
            [20] = Spec("1/2", 4, 65),
            [25] = Spec("1/2", 4, 65),
            [32] = Spec("1/2", 4, 70),
            [40] = Spec("1/2", 4, 70),
            [50] = Spec("5/8", 4, 85),
            [65] = Spec("5/8", 4, 90),
            [80] = Spec("5/8", 4, 90),
            [90] = Spec("5/8", 8, 90),
            [100] = Spec("5/8", 8, 90),
            [125] = Spec("5/8", 8, 95),
            [150] = Spec("3/4", 8, 100),
            [200] = Spec("3/4", 8, 110),
            [250] = Spec("7/8", 12, 115),
            [300] = Spec("7/8", 12, 120),
            [350] = Spec("1", 12, 135),
            [400] = Spec("1", 16, 135),
            [450] = Spec("1-1/8", 16, 145),
        };

        public static bool TryGetRfCl150(int dn, out FlangeBoltingSpec spec) =>
            Class150RfByDn.TryGetValue(dn, out spec!);

        /// <summary>Lap-joint CL150 — same bolt as RF, longer grip (RF + 2× stub lap thickness).</summary>
        public static bool TryGetLjFfCl150(int dn, out FlangeBoltingSpec spec)
        {
            spec = null!;
            if (!Class150RfByDn.TryGetValue(dn, out FlangeBoltingSpec rf))
                return false;

            if (!CatalogStubEndTable.TryGet(dn, CatalogStubEndTable.Pattern.Long, out CatalogStubEndTable.StubEndDims stub))
                return false;

            spec = new FlangeBoltingSpec
            {
                BoltSize = rf.BoltSize,
                NumberInSet = rf.NumberInSet,
                LengthMm = rf.LengthMm + 2.0 * stub.B,
            };
            return true;
        }

        private static FlangeBoltingSpec Spec(string boltInches, int count, double lengthMm) =>
            new()
            {
                BoltSize = $"{boltInches}\"",
                NumberInSet = count,
                LengthMm = lengthMm,
            };
    }
}
