using Edelstein.Data.Transport;
using Edelstein.Security;

using System.Text.Json;

namespace Edelstein.GameServer.Security;

public class EncryptedResponse<T> : EncryptedResponse
{
    public override string EncryptedString { get; }

    public EncryptedResponse(ServerResponse<T> response) =>
        EncryptedString = PayloadCryptor.Encrypt(JsonSerializer.Serialize(response, DefaultSerializerOptions));

    public EncryptedResponse(T response) : this(new ServerResponse<T>(ErrorCode.Success, response)) { }

    public EncryptedResponse(ErrorCode code, T response) : this(new ServerResponse<T>(code, response)) { }

    public EncryptedResponse(GameLibErrorCode code, T response) : this(new ServerResponse<T>(code, response)) { }
}
