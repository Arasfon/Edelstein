using OneOf;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Edelstein.Models.Serialization;

public class OneOfListOrObjectJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        Type genericType = typeToConvert.GetGenericTypeDefinition();
        Type[] genericArgs = typeToConvert.GetGenericArguments();

        return genericType == typeof(OneOf<,>) &&
            genericArgs[0].IsGenericType &&
            genericArgs[0].GetGenericTypeDefinition() == typeof(List<>) &&
            genericArgs[0].GetGenericArguments()[0] == genericArgs[1];
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type valueType = typeToConvert.GetGenericArguments()[1];
        Type converterType = typeof(OneOfListOrObjectJsonConverter<>).MakeGenericType(valueType);
        JsonConverter converter = (JsonConverter)Activator.CreateInstance(converterType)!;
        return converter;
    }
}
