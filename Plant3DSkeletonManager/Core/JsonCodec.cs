using System.Text.Json;
using System.Text.Json.Serialization;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>Serialization used both for the document store and the JSON export.</summary>
    public static class JsonCodec
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string Serialize(ValveProject project) =>
            JsonSerializer.Serialize(project, Options);

        public static ValveProject? Deserialize(string json)
        {
            try { return JsonSerializer.Deserialize<ValveProject>(json, Options); }
            catch (JsonException) { return null; }
        }
    }
}
