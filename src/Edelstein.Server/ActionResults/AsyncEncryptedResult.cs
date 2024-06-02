using Edelstein.Data.Serialization.Json;
using Edelstein.Data.Transport;
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
        TypeInfoResolverChain = { ServerResponseJsonSerializerContext.Default }
    };

    public abstract Task ExecuteResultAsync(ActionContext context);

    public static AsyncEncryptedResult<ServerResponse<object[]>> Create() =>
        Create(StatusCodes.Status200OK, new ServerResponse<object[]>([]));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(T responseData) =>
        Create(StatusCodes.Status200OK, new ServerResponse<T>(responseData));

    public static AsyncEncryptedResult<ServerResponse<object[]>> Create(ErrorCode errorCode) =>
        Create(StatusCodes.Status200OK, new ServerResponse<object[]>(errorCode, []));

    public static AsyncEncryptedResult<ServerResponse<object[]>> Create(GameLibErrorCode errorCode) =>
        Create(StatusCodes.Status200OK, new ServerResponse<object[]>(errorCode, []));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(ErrorCode errorCode, T responseData) =>
        Create(StatusCodes.Status200OK, new ServerResponse<T>(errorCode, responseData));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(GameLibErrorCode errorCode, T responseData) =>
        Create(StatusCodes.Status200OK, new ServerResponse<T>(errorCode, responseData));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(ServerResponse<T> responseData) =>
        Create(StatusCodes.Status200OK, responseData);

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(int statusCode, T responseData) =>
        Create(statusCode, new ServerResponse<T>(responseData));

    public static AsyncEncryptedResult<ServerResponse<object[]>> Create(int statusCode, ErrorCode errorCode) =>
        Create(statusCode, new ServerResponse<object[]>(errorCode, []));

    public static AsyncEncryptedResult<ServerResponse<object[]>> Create(int statusCode, GameLibErrorCode errorCode) =>
        Create(statusCode, new ServerResponse<object[]>(errorCode, []));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(int statusCode, ErrorCode errorCode, T responseData) =>
        Create(statusCode, new ServerResponse<T>(errorCode, responseData));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(int statusCode, GameLibErrorCode errorCode, T responseData) =>
        Create(statusCode, new ServerResponse<T>(errorCode, responseData));

    public static AsyncEncryptedResult<ServerResponse<T>> Create<T>(int statusCode, ServerResponse<T> responseData) =>
        new(statusCode, responseData);
}
