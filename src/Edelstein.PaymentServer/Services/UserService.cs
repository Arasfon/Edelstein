using Edelstein.Data.Models;
using Edelstein.PaymentServer.Models;
using Edelstein.PaymentServer.Repositories;

namespace Edelstein.PaymentServer.Services;

public class UserService : IUserService
{
    private readonly ISequenceRepository<ulong> _sequenceRepository;
    private readonly IAuthenticationDataRepository _authenticationDataRepository;
    private readonly IUserDataRepository _userDataRepository;
    private readonly IUserHomeRepository _userHomeRepository;
    private readonly IUserMissionsRepository _userMissionsRepository;

    public UserService(ISequenceRepository<ulong> sequenceRepository, IAuthenticationDataRepository authenticationDataRepository,
        IUserDataRepository userDataRepository, IUserHomeRepository userHomeRepository, IUserMissionsRepository userMissionsRepository)
    {
        _sequenceRepository = sequenceRepository;
        _authenticationDataRepository = authenticationDataRepository;
        _userDataRepository = userDataRepository;
        _userHomeRepository = userHomeRepository;
        _userMissionsRepository = userMissionsRepository;
    }

    public async Task<UserRegistrationResult> RegisterUser(string publicKey)
    {
        AuthenticationData authenticationData = await _authenticationDataRepository.Create(await GetNextXuid(), publicKey);
        UserData userData = await _userDataRepository.CreateTutorialUserData(authenticationData.Xuid);
        await _userHomeRepository.Create(authenticationData.Xuid);
        await _userMissionsRepository.Create(authenticationData.Xuid);

        return new UserRegistrationResult(authenticationData, userData);
    }

    public async Task<AuthenticationData?> GetAuthenticationDataByUserId(Guid userId) =>
        await _authenticationDataRepository.GetByUserId(userId);

    private async Task<ulong> GetNextXuid() =>
        await _sequenceRepository.GetNextValueById("xuids", 10000_00000_00000);
}
