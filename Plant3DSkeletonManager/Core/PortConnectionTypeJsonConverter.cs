using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>Reads legacy enum names (FLANGED, BUTT_WELD, …) and Plant 3D end codes (FL, BV, …).</summary>
    public sealed class PortConnectionTypeJsonConverter : JsonConverter<PortConnectionType>
    {
        public override PortConnectionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                return PortConnectionType.FL;

            string? raw = reader.GetString();
            return PortConnectionTypeHelper.Parse(raw);
        }

        public override void Write(Utf8JsonWriter writer, PortConnectionType value, JsonSerializerOptions options) =>
            writer.WriteStringValue(PortConnectionTypeHelper.ToEndTypeCode(value));
    }
}
