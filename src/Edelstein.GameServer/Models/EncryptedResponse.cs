using Edelstein.Models;
using Edelstein.Models.Protocol;

using System.Text.Json;

namespace Edelstein.GameServer.Models;

public abstract class EncryptedResponse
{
    public abstract string EncryptedString { get; }

    protected static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}
