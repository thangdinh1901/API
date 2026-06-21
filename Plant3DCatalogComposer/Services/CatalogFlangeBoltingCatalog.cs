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

        /// <summary>Lap-joint CL150 FF — L = ceil(T + 2×tf + 4×H + 6×pitch).</summary>
        public static bool TryGetLjFfCl150(int dn, out FlangeBoltingSpec spec)
        {
            spec = null!;
            if (!Class150RfByDn.TryGetValue(dn, out FlangeBoltingSpec rf))
                return false;
            if (!CatalogLjRingCl150Table.TryGet(dn, out CatalogLjRingCl150Table.LjRingDims ring))
                return false;

            string bolt = rf.BoltSize.TrimEnd('"');
            double h = NutThicknessMm(bolt);
            double grip = DefaultGasketThicknessMm + 2.0 * ring.Tf + 2.0 * h;
            double inset = StudBearingInsetMm(bolt);
            spec = new FlangeBoltingSpec
            {
                BoltSize = rf.BoltSize,
                NumberInSet = rf.NumberInSet,
                LengthMm = Math.Ceiling(grip + 2.0 * inset),
            };
            return true;
        }

        private const double DefaultGasketThicknessMm = 1.5;

        private static double NutThicknessMm(string boltInches) =>
            boltInches switch
            {
                "1/2" => 12.303,
                "5/8" => 15.478,
                "3/4" => 18.653,
                "7/8" => 21.828,
                "1" => 25.003,
                "1-1/8" => 28.178,
                _ => 15.478,
            };

        private static double NutPitchMm(string boltInches) =>
            boltInches switch
            {
                "1/2" => 1.954,
                "5/8" => 2.309,
                "3/4" => 2.540,
                "7/8" => 2.822,
                "1" => 3.175,
                "1-1/8" => 3.175,
                _ => 2.309,
            };

        private static double StudBearingInsetMm(string boltInches) =>
            NutThicknessMm(boltInches) + 3.0 * NutPitchMm(boltInches);

        private static FlangeBoltingSpec Spec(string boltInches, int count, double lengthMm) =>
            new()
            {
                BoltSize = $"{boltInches}\"",
                NumberInSet = count,
                LengthMm = lengthMm,
            };
    }
}
