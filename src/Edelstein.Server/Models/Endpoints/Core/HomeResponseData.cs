using Edelstein.Data.Models;
using Edelstein.Data.Serialization.Json;

using System.Text.Json.Serialization;

namespace Edelstein.Server.Models.Endpoints.Core;

public record HomeResponseData(
    Home Home,
    List<uint> ClearMissionIds
);

[JsonSourceGenerationOptions(Converters = [typeof(BooleanToIntegerJsonConverter), typeof(OneOfListOrObjectJsonConverterFactory)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(HomeResponseData))]
public partial class HomeResponseDataJsonSerializerContext : JsonSerializerContext;
