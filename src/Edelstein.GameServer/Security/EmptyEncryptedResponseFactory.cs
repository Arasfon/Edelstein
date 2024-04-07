using Edelstein.Data.Transport;

namespace Edelstein.GameServer.Security;

public class EmptyEncryptedResponseFactory
{
    public static EncryptedResponse<List<object>> Create() =>
        new(new ServerResponse<List<object>>(ErrorCode.Success, []));

    public static EncryptedResponse<List<object>> Create(ErrorCode code) =>
        new(new ServerResponse<List<object>>(code, []));

    public static EncryptedResponse<List<object>> Create(GameLibErrorCode code) =>
        new(new ServerResponse<List<object>>(code, []));
}
