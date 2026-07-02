using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Scene part fingerprint embedded in exported CUST_*.py for deploy/test verification.</summary>
    internal static class CatalogSceneManifest
    {
        public const string MarkerPrefix = "# P3D_SCENE_MANIFEST:";

        private static readonly Regex ManifestLine = new(
            @"^#\s*P3D_SCENE_MANIFEST:\s*(?<body>.+)$",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static string Build(ValveProject project)
        {
            string parts = string.Join(
                ", ",
                project.Parts.Select(p => p.Name));
            return $"{MarkerPrefix} {project.Parts.Count} part(s): {parts}";
        }

        public static bool TryReadFromScript(string scriptPath, out string? manifestLine)
        {
            manifestLine = null;
            if (!File.Exists(scriptPath))
                return false;

            Match match = ManifestLine.Match(File.ReadAllText(scriptPath));
            if (!match.Success)
                return false;

            manifestLine = match.Value.Trim();
            return true;
        }

        public static bool MatchesProject(ValveProject project, string scriptPath, out string message)
        {
            if (!TryReadFromScript(scriptPath, out string? deployed))
            {
                message =
                    "Deployed script has no scene manifest — run Deploy Catalog after scene changes "
                    + "(Generate Code alone does not update CustomScripts unless dev deploy is configured).";
                return false;
            }

            string expected = Build(project);
            if (string.Equals(deployed, expected, StringComparison.Ordinal))
            {
                message = expected;
                return true;
            }

            message =
                "Deployed script does not match the saved scene.\n\n"
                + $"Scene JSON:  {expected}\n"
                + $"CustomScripts: {deployed}\n\n"
                + "Run Deploy Catalog (not only Generate Code) after editing the Scene tree.";
            return false;
        }
    }
}
