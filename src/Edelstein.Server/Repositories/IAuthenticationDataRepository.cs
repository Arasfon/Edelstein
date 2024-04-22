using Edelstein.Data.Models;

namespace Edelstein.Server.Repositories;

public interface IAuthenticationDataRepository
{
    public Task<AuthenticationData> Create(ulong xuid, string publicKey);
    public Task<AuthenticationData?> GetByUserId(Guid userId);
    public Task<AuthenticationData?> GetByXuid(ulong xuid);
}
