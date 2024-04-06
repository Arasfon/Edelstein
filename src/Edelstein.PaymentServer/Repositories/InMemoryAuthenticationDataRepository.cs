using Edelstein.Data.Models;

namespace Edelstein.PaymentServer.Repositories;

public class InMemoryAuthenticationDataRepository : IAuthenticationDataRepository
{
    private readonly Dictionary<Guid, AuthenticationData> _authenticationDatas = new();

    public Task<AuthenticationData> Create(ulong xuid, string publicKey)
    {
        AuthenticationData createdAuthenticationData = new(Guid.NewGuid(), xuid, publicKey);

        _authenticationDatas[createdAuthenticationData.UserId] = createdAuthenticationData;

        return Task.FromResult(createdAuthenticationData);
    }

    public Task<AuthenticationData?> GetByUserId(Guid userId)
    {
        _authenticationDatas.TryGetValue(userId, out AuthenticationData? authenticationData);
        return Task.FromResult(authenticationData);
    }
}
