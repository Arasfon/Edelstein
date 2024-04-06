using Edelstein.Data.Transport;

namespace Edelstein.GameServer.Security;

public class EmptyEncryptedResponseFactory
{
    public static EncryptedResponse<object?> Create(ErrorCode code) =>
        new(new ServerResponse<object?>(code, null));

    public static EncryptedResponse<object?> Create(GameLibErrorCode code) =>
        new(new ServerResponse<object?>(code, null));
}
