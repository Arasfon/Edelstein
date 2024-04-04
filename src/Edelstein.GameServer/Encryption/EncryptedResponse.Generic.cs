using Edelstein.Data.Transport;
using Edelstein.Security;

using System.Text.Json;

namespace Edelstein.GameServer.Encryption;

public class EncryptedResponse<T> : EncryptedResponse
{
    public override string EncryptedString { get; }

    public EncryptedResponse(ServerResponse<T> response) =>
        EncryptedString = PayloadCryptor.Encrypt(JsonSerializer.Serialize(response, DefaultSerializerOptions));

    public EncryptedResponse(ErrorCode code, T response) : this(new ServerResponse<T>(code, response)) { }

    public EncryptedResponse(GameLibErrorCode code, T response) : this(new ServerResponse<T>(code, response)) { }

    public static EncryptedResponse<object?> CreateEmpty(ErrorCode code) =>
        new(new ServerResponse<object?>(code, null));

    public static EncryptedResponse<object?> CreateEmpty(GameLibErrorCode code) =>
        new(new ServerResponse<object?>(code, null));
}
