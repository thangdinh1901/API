using System;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogFlangeFacing
    {
        public static readonly string[] Options = ["RF", "FF"];

        public static string Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "RF";

            return raw.Trim().ToUpperInvariant() switch
            {
                "FF" or "FLAT" or "FLAT FACE" => "FF",
                _ => "RF",
            };
        }

        public static bool IsFlangedEndType(string? endType)
        {
            string code = Plant3DEndTypes.NormalizeCode(endType);
            return code is "FL" or "WF" or "LFL";
        }

        public static bool IsButtWeldEndType(string? endType)
        {
            string code = Plant3DEndTypes.NormalizeCode(endType);
            return code is "BV" or "PL" or "PPL" or "P";
        }

        public static bool PrimaryEndUsesFacing(string? primaryEndType)
        {
            string code = Plant3DEndTypes.NormalizeCode(primaryEndType);
            return code is "FL" or "WF" or "LFL";
        }
    }
}
