using Edelstein.Data.Serialization.Json;
using Edelstein.Data.Transport;
using Edelstein.Server.Models.Endpoints.Core;

using System.Text.Json.Serialization;

namespace Edelstein.Server.Models.Serializers;

[JsonSourceGenerationOptions(Converters = [typeof(BooleanToIntegerJsonConverter), typeof(OneOfListOrObjectJsonConverterFactory)],
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ServerResponse<HomeResponseData>))]
public partial class ServerResponseHomeJsonSerializerContext : JsonSerializerContext;
