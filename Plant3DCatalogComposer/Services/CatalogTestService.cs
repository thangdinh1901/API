using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
        private static int _testSequence;

        public static CatalogTestResult BuildTestCommand(string dwgPath)
        {
            ValveProject project = SceneGraphCatalogService.ReloadScene(dwgPath);
            CatalogExportPrepareService.PrepareSceneForExport(project);

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

            if (!CatalogSceneManifest.TryReadFromScript(pyPath, out _))
            {
                return new CatalogTestResult
                {
                    CanRun = true,
                    ScriptName = scriptName,
                    CommandLine = BuildInvoke(scriptName, args, out _),
                    Message =
                        $"Testing {scriptName} — deployed script has no scene manifest (re-run Deploy Catalog after plugin update).{Environment.NewLine}"
                        + $"Current scene: {CatalogSceneManifest.Build(project)}",
                };
            }

            if (!CatalogSceneManifest.MatchesProject(project, pyPath, out string manifestMessage))
            {
                return new CatalogTestResult
                {
                    CanRun = false,
                    ScriptName = scriptName,
                    Message = manifestMessage,
                };
            }

            string command = BuildInvoke(scriptName, args, out _);
            return new CatalogTestResult
            {
                CanRun = true,
                ScriptName = scriptName,
                CommandLine = command,
                Message =
                    $"Testing {scriptName} — {manifestMessage}{Environment.NewLine}"
                    + "Preview geometry will be erased before test so only catalog script geometry remains.",
            };
        }

        private static string BuildInvoke(
            string scriptName,
            IReadOnlyList<(string Name, string Value)> args,
            out int seq)
        {
            var parts = new List<string> { $"(testacpscript \"{scriptName}\"" };
            foreach ((string name, string value) in args)
            {
                parts.Add($"\"{name}\"");
                parts.Add($"\"{value}\"");
            }

            parts.Add(")");
            string invoke = string.Join(" ", parts);
            seq = Interlocked.Increment(ref _testSequence);
            return WrapperScript.WrapTestCatalogInvoke(seq, invoke, registerScripts: true);
        }

        public static bool TryQueueTest(Document? doc, string dwgPath)
        {
            CatalogTestResult test = BuildTestCommand(dwgPath);
            if (!test.CanRun || doc == null)
                return false;

            doc.Editor.WriteMessage($"\nP3D Composer: {test.Message}");
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

            foreach ((string name, double value) in CatalogExportPrepareService.CollectExportDimensionParams(project))
            {
                list.Add((name, value.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            if (xmlDefaults != null)
            {
                foreach (KeyValuePair<string, string> kv in xmlDefaults)
                {
                    if (kv.Key.Equals("DN", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (list.Any(a => a.Item1.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)))
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

            double fromProject = ProjectDimensionService.GetValue(project, name);
            if (fromProject > 0)
                return fromProject.ToString("0.###", CultureInfo.InvariantCulture);

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
