using Edelstein.Protocol;

using System.Text.Json.Serialization;

namespace Edelstein.GameServer.Models;

public class ServerResponse<T>(T data)
{
    [JsonPropertyName("code")]
    public ErrorCode ErrorCode { get; init; } = ErrorCode.Ok;

    [JsonPropertyName("server_time")]
    public long ServerTimestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public T Data { get; set; } = data;

    public ServerResponse(ErrorCode code, T data) : this(data) =>
        ErrorCode = code;
}
