using Edelstein.Data.Models;
using Edelstein.Data.Serialization.Json;
using Edelstein.Server.Models.Endpoints.Core;
using Edelstein.Server.Models.Serializers;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Edelstein.Server.ActionResults;

public abstract class AsyncEncryptedResult : IActionResult
{
    protected static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new BooleanToIntegerJsonConverter(),
            new OneOfListOrObjectJsonConverterFactory()
        },
        TypeInfoResolverChain =
        {
            HomeJsonSerializerContext.Default,
            HomeResponseDataJsonSerializerContext.Default,
            ServerResponseHomeJsonSerializerContext.Default
        }
    };

    public abstract Task ExecuteResultAsync(ActionContext context);
}
