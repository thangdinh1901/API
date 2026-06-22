using System;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogStandardSetInference
    {
        public static string ResolvePrimaryEndType(ValveProject project)
        {
            if (!string.IsNullOrWhiteSpace(project.PrimaryEndType))
                return Plant3DEndTypes.NormalizeCode(project.PrimaryEndType);

            if (!string.IsNullOrWhiteSpace(project.StandardSet))
                return PrimaryEndFromStandardSet(project.StandardSet);

            string partId = CatalogProjectService.SanitizeCatalogName(project.ValveName);
            return PrimaryEndFromPartId(partId);
        }

        public static string InferStandardSet(ValveProject project, string? primaryEndType = null)
        {
            string pet = Plant3DEndTypes.NormalizeCode(primaryEndType ?? ResolvePrimaryEndType(project));
            string schedule = PipeScheduleCatalog.Normalize(project.Parameters.PipeSchedule ?? "");
            string partId = CatalogProjectService.SanitizeCatalogName(project.ValveName);

            if (pet.Equals("BV", StringComparison.OrdinalIgnoreCase)
                || pet.Equals("PL", StringComparison.OrdinalIgnoreCase))
            {
                if (schedule is "40" or "40S" or "")
                    return BwSch40StandardCatalog.SetId;
            }

            if (pet.Equals("SW", StringComparison.OrdinalIgnoreCase)
                || pet.Equals("PSW", StringComparison.OrdinalIgnoreCase))
            {
                return SwCl3000StandardCatalog.SetId;
            }

            if (pet.Equals("LAP", StringComparison.OrdinalIgnoreCase)
                || pet.Equals("LLP", StringComparison.OrdinalIgnoreCase))
            {
                if (partId.Contains("LJ", StringComparison.OrdinalIgnoreCase)
                    || partId.Contains("LAP", StringComparison.OrdinalIgnoreCase))
                {
                    return "LAP_JOINT_CL150";
                }
            }

            return InferStandardSetFromPartId(partId);
        }

        public static string PrimaryEndFromStandardSet(string? standardSet)
        {
            if (string.IsNullOrWhiteSpace(standardSet))
                return "Undefined_ET";

            return standardSet.Trim() switch
            {
                var id when id.Equals(BwSch40StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase) => "BV",
                var id when id.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase) => "SW",
                "LAP_JOINT_CL150" => "LAP",
                _ => "Undefined_ET",
            };
        }

        private static string PrimaryEndFromPartId(string partId)
        {
            string id = partId.ToUpperInvariant();
            if (id.Contains("_SW_", StringComparison.Ordinal) || id.EndsWith("_SW", StringComparison.Ordinal))
                return "SW";
            if (id.Contains("_BW_", StringComparison.Ordinal) || id.Contains("SCH40", StringComparison.Ordinal))
                return "BV";
            if (id.StartsWith("WN_", StringComparison.Ordinal)
                || id.StartsWith("SO_", StringComparison.Ordinal)
                || id.StartsWith("BLD_", StringComparison.Ordinal))
                return "FL";
            if (id.StartsWith("GSK_", StringComparison.Ordinal))
                return "FL";
            if (id.Contains("LJ_", StringComparison.Ordinal) || id.Contains("LAP", StringComparison.Ordinal))
                return "LAP";
            return "Undefined_ET";
        }

        private static string InferStandardSetFromPartId(string partId)
        {
            if (partId.Contains("_BW_SCH40", StringComparison.OrdinalIgnoreCase))
                return BwSch40StandardCatalog.SetId;
            if (partId.Contains("_SW_CL3000", StringComparison.OrdinalIgnoreCase))
                return SwCl3000StandardCatalog.SetId;
            return "";
        }
    }
}
