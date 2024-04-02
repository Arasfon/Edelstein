using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Edelstein.Models.Protocol;

public readonly struct ServerResponse<T>
{
    [JsonPropertyName("code")]
    public required int ErrorCode { get; init; } = (int)Protocol.ErrorCode.Success;

    [JsonPropertyName("server_time")]
    public long ServerTimestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public required T Data { get; init; }

    public ServerResponse() { }

    [SetsRequiredMembers]
    public ServerResponse(T data) =>
        Data = data;

    [SetsRequiredMembers]
    public ServerResponse(ErrorCode code, T data) : this(data) =>
        ErrorCode = (int)code;

    [SetsRequiredMembers]
    public ServerResponse(GameLibErrorCode code, T data) : this(data) =>
        ErrorCode = (int)code;
}
