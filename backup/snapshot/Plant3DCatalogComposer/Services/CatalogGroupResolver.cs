using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Plant 3D Spec Editor treats Group="Valve" as flanged (FL) in the size/connection table
    /// even when FirstPortEndtypes is BV — use Fitting for butt-weld / socket-weld custom parts.
    /// </summary>
    internal static class CatalogGroupResolver
    {
        public static string Resolve(string? userGroup, IReadOnlyList<ConnectionPort> ports, string? firstPortEndtypes)
        {
            string group = string.IsNullOrWhiteSpace(userGroup) ? "Custom" : userGroup.Trim();
            if (ports.Count == 0)
                return group;

            string endtypes = firstPortEndtypes ?? PortConnectionTypeHelper.BuildEndtypesCsv(ports);
            if (!group.Equals("Valve", StringComparison.OrdinalIgnoreCase))
                return group;

            if (IsNonFlangedEndtypes(endtypes))
                return "Fitting";

            return group;
        }

        public static bool WouldRemapValveToFitting(string? userGroup, IReadOnlyList<ConnectionPort> ports)
        {
            if (ports.Count == 0)
                return false;

            string group = string.IsNullOrWhiteSpace(userGroup) ? "Custom" : userGroup.Trim();
            if (!group.Equals("Valve", StringComparison.OrdinalIgnoreCase))
                return false;

            return IsNonFlangedEndtypes(PortConnectionTypeHelper.BuildEndtypesCsv(ports));
        }

        private static bool IsNonFlangedEndtypes(string endtypesCsv)
        {
            foreach (string token in endtypesCsv.Split(','))
            {
                string code = token.Trim();
                if (code.Length == 0)
                    continue;

                if (!code.Equals("FL", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
