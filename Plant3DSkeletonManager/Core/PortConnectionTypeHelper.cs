using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DSkeletonManager.Core
{
    public static class PortConnectionTypeHelper
    {
        public static IReadOnlyList<(PortConnectionType Type, string Description)> CatalogEndTypes { get; } =
            BuildCatalogTypes();

        private static IReadOnlyList<(PortConnectionType Type, string Description)> BuildCatalogTypes()
        {
            var list = new List<(PortConnectionType, string)>();
            foreach ((string code, string description) in Plant3DEndTypes.All)
            {
                if (TryToPortType(code, out PortConnectionType type))
                    list.Add((type, description));
            }

            return list;
        }

        public static string ToEndTypeCode(PortConnectionType type) => type.ToString();

        public static string GetDescription(PortConnectionType type) =>
            Plant3DEndTypes.GetDescription(ToEndTypeCode(type));

        public static string GetDisplayName(PortConnectionType type) =>
            Plant3DEndTypes.FormatDisplay(ToEndTypeCode(type));

        public static string PortLabel(ConnectionPort port) => $"Port {port.Number}";

        public static string PortMarkerLabel(ConnectionPort port) =>
            $"#{port.Number} {ToEndTypeCode(port.Type)}";

        public static PortConnectionType Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return PortConnectionType.FL;

            string code = Plant3DEndTypes.NormalizeCode(raw);
            if (TryToPortType(code, out PortConnectionType parsed))
                return parsed;

            return PortConnectionType.FL;
        }

        public static bool TryToPortType(string? code, out PortConnectionType type)
        {
            type = PortConnectionType.FL;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return Enum.TryParse(Plant3DEndTypes.NormalizeCode(code), ignoreCase: false, out type);
        }

        public static string BuildEndtypesCsv(IEnumerable<ConnectionPort> ports) =>
            string.Join(",", ports.OrderBy(p => p.Number).Select(p => ToEndTypeCode(p.Type)));
    }
}
