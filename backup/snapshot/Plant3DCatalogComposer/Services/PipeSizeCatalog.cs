using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Nominal pipe sizes (DN mm + NPS inch) with ASME B36.10 Sch-40 OD (mm).</summary>
    public static class PipeSizeCatalog
    {
        public static IReadOnlyList<PipeSizeOption> NominalSizes { get; } =
        [
            new(15, "1/2", 21.3),
            new(20, "3/4", 26.7),
            new(25, "1", 33.4),
            new(32, "1-1/4", 42.2),
            new(40, "1-1/2", 48.3),
            new(50, "2", 60.3),
            new(65, "2-1/2", 73.0),
            new(80, "3", 88.9),
            new(90, "3-1/2", 101.6),
            new(100, "4", 114.3),
            new(125, "5", 141.3),
            new(150, "6", 168.3),
            new(200, "8", 219.1),
            new(250, "10", 273.0),
            new(300, "12", 323.8),
            new(350, "14", 355.6),
            new(400, "16", 406.4),
            new(450, "18", 457.2),
        ];

        public static IReadOnlyList<string> PressureClasses { get; } = ["150", "300", "3000"];

        public static PipeSizeOption? FindByDn(double dnMm) =>
            NominalSizes.FirstOrDefault(s => System.Math.Abs(s.DnMm - dnMm) < 0.5);

        public static double OdSch40Mm(double dnMm) =>
            FindByDn(dnMm)?.OdSch40Mm ?? dnMm;
    }

    public sealed class PipeSizeOption(int dnMm, string npsLabel, double odSch40Mm)
    {
        public int DnMm { get; } = dnMm;

        public string NpsLabel { get; } = npsLabel;

        /// <summary>Outside diameter (mm), ASME B36.10 Sch-40.</summary>
        public double OdSch40Mm { get; } = odSch40Mm;

        public string Display => $"DN{DnMm} ({NpsLabel}\")";

        public override string ToString() => Display;
    }
}
