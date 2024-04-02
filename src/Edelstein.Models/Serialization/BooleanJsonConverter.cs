using System.Text.Json;
using System.Text.Json.Serialization;

namespace Edelstein.Models.Serialization;

public class BooleanJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => reader.GetString() switch
            {
                "true" => true,
                "false" => false,
                _ => throw new JsonException()
            },
            JsonTokenType.Number => Convert.ToBoolean(reader.GetInt64()),
            _ => throw new JsonException($"Unexpected JSON token type: {reader.TokenType}")
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(value);
}
