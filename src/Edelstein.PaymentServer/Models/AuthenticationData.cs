using MongoDB.Bson.Serialization.Attributes;

namespace Edelstein.PaymentServer.Models;

public class AuthenticationData
{
    [BsonId]
    public Guid UserId { get; }

    public ulong Xuid { get; }
    public string PublicKey { get; }

    public AuthenticationData(Guid userId, ulong xuid, string publicKey)
    {
        UserId = userId;
        Xuid = xuid;
        PublicKey = publicKey;
    }

    public string UserIdString => UserId.ToString().Replace("-", "");
}
