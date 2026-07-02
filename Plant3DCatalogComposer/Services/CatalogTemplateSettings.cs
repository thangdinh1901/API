using System;
using System.IO;
using System.Text.Json;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// User-configurable Catalog Builder Excel template path. When set to an existing file it
    /// overrides the bundled Resources/CatalogBuilderTemplate.xlsx so a real, validated catalog
    /// workbook (e.g. CATA_NUI.xlsx) can be used as the standard template + clone source.
    /// Persisted to %AppData%\Plant3DCatalogComposer\template.json.
    /// </summary>
    internal sealed class CatalogTemplateSettings
    {
        public string? TemplatePath { get; set; }

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Plant3DCatalogComposer",
            "template.json");

        private static readonly object Gate = new();
        private static CatalogTemplateSettings? _cache;

        /// <summary>Configured template path if it exists on disk, otherwise null.</summary>
        public static string? ResolveConfiguredTemplatePath()
        {
            string? path = Load().TemplatePath;
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = Environment.ExpandEnvironmentVariables(path.Trim());
            return File.Exists(path) ? path : null;
        }

        public static CatalogTemplateSettings Load()
        {
            lock (Gate)
            {
                if (_cache != null)
                    return _cache;

                try
                {
                    if (File.Exists(SettingsPath))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        _cache = JsonSerializer.Deserialize<CatalogTemplateSettings>(
                            File.ReadAllText(SettingsPath),
                            options);
                    }
                }
                catch
                {
                    // fall through to default
                }

                return _cache ??= new CatalogTemplateSettings();
            }
        }

        public static void Save(string? templatePath)
        {
            var settings = new CatalogTemplateSettings
            {
                TemplatePath = string.IsNullOrWhiteSpace(templatePath) ? null : templatePath.Trim(),
            };

            lock (Gate)
            {
                try
                {
                    string dir = Path.GetDirectoryName(SettingsPath)!;
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(
                        SettingsPath,
                        JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                    _cache = settings;
                }
                catch
                {
                    // best-effort persistence
                }
            }
        }
    }
}
