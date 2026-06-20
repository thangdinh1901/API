using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogTestResult
    {
        public bool CanRun { get; init; }
        public string ScriptName { get; init; } = string.Empty;
        public string CommandLine { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    internal static class CatalogTestService
    {
        public static CatalogTestResult BuildTestCommand(ValveProject project)
        {
            string scriptName = CatalogProjectService.PreviewScriptName(project);
            string pyPath = Path.Combine(ProjectPaths.CustomScriptsDir, scriptName + ".py");
            if (!File.Exists(pyPath))
            {
                return new CatalogTestResult
                {
                    CanRun = false,
                    ScriptName = scriptName,
                    Message = $"Script not deployed: {pyPath}\nRun Deploy Catalog first.",
                };
            }

            string partId = scriptName.StartsWith("CUST_", StringComparison.Ordinal)
                ? scriptName[5..]
                : scriptName;
            IReadOnlyList<(string Name, string Value)> args = ResolveTestArguments(project, partId);
            var parts = new List<string> { $"(testacpscript \"{scriptName}\"" };
            foreach ((string name, string value) in args)
            {
                parts.Add($"\"{name}\"");
                parts.Add($"\"{value}\"");
            }

            parts.Add(")");
            string command = string.Join(" ", parts);
            return new CatalogTestResult
            {
                CanRun = true,
                ScriptName = scriptName,
                CommandLine = command,
                Message = command,
            };
        }

        public static bool TryQueueTest(Document? doc, ValveProject project)
        {
            CatalogTestResult test = BuildTestCommand(project);
            if (!test.CanRun || doc == null)
                return false;

            doc.Editor.WriteMessage($"\nP3D Composer: {test.CommandLine}");
            doc.SendStringToExecute(test.CommandLine + "\n", true, false, false);
            return true;
        }

        private static IReadOnlyList<(string Name, string Value)> ResolveTestArguments(
            ValveProject project,
            string partId)
        {
            var list = new List<(string, string)>();
            int dn = project.Parameters.DN > 0
                ? (int)Math.Round(project.Parameters.DN)
                : 100;

            Dictionary<string, string>? xmlDefaults = TryLoadParamDefaults(partId);
            if (xmlDefaults != null && xmlDefaults.TryGetValue("DN", out string? dnDefault))
            {
                if (project.Parameters.DN <= 0 && int.TryParse(dnDefault, out int parsed))
                    dn = parsed;
            }

            list.Add(("DN", dn.ToString(CultureInfo.InvariantCulture)));

            if (xmlDefaults != null)
            {
                foreach (KeyValuePair<string, string> kv in xmlDefaults)
                {
                    if (kv.Key.Equals("DN", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string value = ResolveParamValue(project, kv.Key, kv.Value);
                    list.Add((kv.Key, value));
                }
            }

            return list;
        }

        private static string ResolveParamValue(ValveProject project, string name, string defaultValue)
        {
            if (name.Equals("DN2", StringComparison.OrdinalIgnoreCase))
            {
                int dn2 = project.Parameters.DN2 > 0
                    ? (int)Math.Round(project.Parameters.DN2)
                    : BwFittingSizeCatalog.DefaultReducerSmallDn((int)Math.Round(project.Parameters.DN));
                return dn2.ToString(CultureInfo.InvariantCulture);
            }

            if (name.Equals("CEL", StringComparison.OrdinalIgnoreCase))
                return "0";

            return defaultValue;
        }

        private static Dictionary<string, string>? TryLoadParamDefaults(string partId)
        {
            string? partsDir = ProjectPaths.TryResolveDevPartsDir() ?? ProjectPaths.PartsDir;
            string xmlPath = Path.Combine(partsDir, partId, "catalog_entry.xml");
            if (!File.Exists(xmlPath))
                return null;

            try
            {
                XDocument doc = XDocument.Load(xmlPath);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement param in doc.Descendants("Param"))
                {
                    string? name = param.Attribute("Name")?.Value;
                    string? value = param.Attribute("Value")?.Value;
                    if (!string.IsNullOrEmpty(name) && value != null)
                        dict[name] = value;
                }

                return dict.Count > 0 ? dict : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
