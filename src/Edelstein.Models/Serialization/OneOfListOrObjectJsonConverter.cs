using OneOf;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Edelstein.Models.Serialization;

public class OneOfListOrObjectJsonConverter<T> : JsonConverter<OneOf<List<T>, T>>
{
    public override OneOf<List<T>, T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartArray:
            {
                List<T> list = JsonSerializer.Deserialize<List<T>>(ref reader, options)!;
                return list;
            }
            case JsonTokenType.StartObject:
            {
                T obj = JsonSerializer.Deserialize<T>(ref reader, options)!;
                return obj;
            }
            default:
                throw new JsonException($"Unexpected JSON token type: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, OneOf<List<T>, T> value, JsonSerializerOptions options) =>
        value.Switch(list => JsonSerializer.Serialize(writer, list, options),
            obj => JsonSerializer.Serialize(writer, obj, options));
}
