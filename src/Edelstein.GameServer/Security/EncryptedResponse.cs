using System.Text.Json;

namespace Edelstein.GameServer.Security;

public abstract class EncryptedResponse
{
    public abstract string EncryptedString { get; }

    protected static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}