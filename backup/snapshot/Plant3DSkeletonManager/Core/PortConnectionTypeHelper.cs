using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DSkeletonManager.Core
{
    public static class PortConnectionTypeHelper
    {
        private static readonly (PortConnectionType Type, string Description)[] CatalogTypes =
        {
            (PortConnectionType.FL, "Flanged"),
            (PortConnectionType.BV, "Beveled (butt weld)"),
            (PortConnectionType.PL, "Plain"),
            (PortConnectionType.SW, "Socket weld"),
            (PortConnectionType.THDM, "Threaded male"),
            (PortConnectionType.THDF, "Threaded female"),
            (PortConnectionType.SO, "Slip-on"),
            (PortConnectionType.WF, "Wafer"),
            (PortConnectionType.LAP, "Lap joint"),
            (PortConnectionType.GRV, "Grooved"),
        };

        public static IReadOnlyList<(PortConnectionType Type, string Description)> CatalogEndTypes => CatalogTypes;

        public static string ToEndTypeCode(PortConnectionType type) => type.ToString();

        public static string GetDescription(PortConnectionType type)
        {
            foreach ((PortConnectionType t, string description) in CatalogTypes)
            {
                if (t == type)
                    return description;
            }

            return type.ToString();
        }

        public static string GetDisplayName(PortConnectionType type) =>
            $"{ToEndTypeCode(type)} — {GetDescription(type)}";

        public static string PortLabel(ConnectionPort port) => $"Port {port.Number}";

        public static string PortMarkerLabel(ConnectionPort port) =>
            $"#{port.Number} {ToEndTypeCode(port.Type)}";

        public static PortConnectionType Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return PortConnectionType.FL;

            string value = raw.Trim();
            if (Enum.TryParse(value, true, out PortConnectionType parsed))
                return parsed;

            return value.ToUpperInvariant() switch
            {
                "FLANGED" => PortConnectionType.FL,
                "BUTT_WELD" or "BW" => PortConnectionType.BV,
                "SOCKET_WELD" => PortConnectionType.SW,
                "THREADED" or "TH" => PortConnectionType.THDM,
                "CUSTOM" => PortConnectionType.FL,
                _ => PortConnectionType.FL,
            };
        }

        public static string BuildEndtypesCsv(IEnumerable<ConnectionPort> ports) =>
            string.Join(",", ports.OrderBy(p => p.Number).Select(p => ToEndTypeCode(p.Type)));
    }
}
