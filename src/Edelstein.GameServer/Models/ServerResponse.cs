using System.Text.Json.Serialization;

namespace Edelstein.GameServer.Models;

public class ServerResponse<T>(T data)
{
    [JsonPropertyName("code")]
    public int ErrorCode { get; init; }

    [JsonPropertyName("server_time")]
    public long ServerTimestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public T Data { get; set; } = data;

    public ServerResponse(int code, T data) : this(data) =>
        ErrorCode = code;
}
