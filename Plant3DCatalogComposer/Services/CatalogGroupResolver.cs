using System;
using System.Collections.Generic;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Plant @activate Group from Part Family. User-selected Valve stays Valve
    /// (FirstPortEndtypes carries BV/SW/FL — do not remap to Fitting).
    /// </summary>
    internal static class CatalogGroupResolver
    {
        public static string Resolve(string? userGroup, IReadOnlyList<ConnectionPort> ports, string? firstPortEndtypes)
        {
            _ = ports;
            _ = firstPortEndtypes;
            return string.IsNullOrWhiteSpace(userGroup) ? "Custom" : userGroup.Trim();
        }

        public static bool WouldRemapValveToFitting(string? userGroup, IReadOnlyList<ConnectionPort> ports) =>
            false;
    }
}
