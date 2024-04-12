using Edelstein.Data.Models;
using Edelstein.PaymentServer.Models;

namespace Edelstein.PaymentServer.Services;

public interface IUserService
{
    public Task<UserRegistrationResult> RegisterUser(string publicKey);
    public Task<AuthenticationData?> GetAuthenticationDataByUserId(Guid userId);
    public Task<UserData> GetUserDataByXuid(ulong xuid);
}
