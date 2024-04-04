using Edelstein.PaymentServer.Models;

namespace Edelstein.PaymentServer.Repositories;

public interface IAuthenticationDataRepository
{
    public Task<AuthenticationData> Create(ulong xuid, string publicKey);
    public Task<AuthenticationData?> GetByUserId(Guid userId);
}
