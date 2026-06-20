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
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Plant3DCatalogComposer",
                "deploy.json");
            if (!File.Exists(path))
                return null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<DeploySettings>(File.ReadAllText(path), options);
            }
            catch
            {
                return null;
            }
        }
    }
}
