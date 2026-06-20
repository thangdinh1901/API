using System;
using System.IO;
using System.Text.Json;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class DeploySettings
    {
        public string? ApiRoot { get; set; }
        public string? CatalogGenerator { get; set; }
        public string? PrimitivesPy { get; set; }

        public static DeploySettings? TryLoad()
        {
            foreach (string path in CandidatePaths())
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    DeploySettings? settings = JsonSerializer.Deserialize<DeploySettings>(
                        File.ReadAllText(path),
                        options);
                    if (settings != null && !string.IsNullOrWhiteSpace(settings.CatalogGenerator))
                        return settings;
                }
                catch
                {
                    // try next candidate
                }
            }

            return null;
        }

        private static string[] CandidatePaths()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Plant3DCatalogComposer",
                "deploy.json");

            string pluginDir = ProjectPaths.PluginDirectory;
            string bundle = Path.Combine(pluginDir, "deploy.json");
            string customScripts = Path.Combine(ProjectPaths.CustomScriptsDir, "deploy.json");

            return new[] { appData, bundle, customScripts };
        }
    }
}
