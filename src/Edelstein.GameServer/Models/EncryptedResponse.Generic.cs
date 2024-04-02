using Edelstein.Models;
using Edelstein.Models.Protocol;
using Edelstein.Protocol;

using System.Text.Json;

namespace Edelstein.GameServer.Models;

public class EncryptedResponse<T> : EncryptedResponse
{
    private readonly ServerResponse<T> _reponse;
    public override string EncryptedString { get; }

    public EncryptedResponse(ServerResponse<T> response)
    {
        _reponse = response;
        EncryptedString = ProtocolCryptor.Encrypt(JsonSerializer.Serialize(response, DefaultSerializerOptions));
    }

    public EncryptedResponse(ErrorCode code, T response) : this(new ServerResponse<T>(code, response)) { }

    public EncryptedResponse(GameLibErrorCode code, T response) : this(new ServerResponse<T>(code, response)) { }

    public static EncryptedResponse<object?> CreateEmpty(ErrorCode code) =>
        new(new ServerResponse<object?>(code, null));

    public static EncryptedResponse<object?> CreateEmpty(GameLibErrorCode code) =>
        new(new ServerResponse<object?>(code, null));

    public static implicit operator EncryptedResponse<T>(ServerResponse<T> response) =>
        new(response);
}
