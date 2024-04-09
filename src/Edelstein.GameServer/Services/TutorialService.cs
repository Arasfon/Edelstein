using Edelstein.GameServer.Repositories;

namespace Edelstein.GameServer.Services;

public class TutorialService : ITutorialService
{
    private readonly HashSet<ulong> _usersInTutorial = [];

    private readonly IUserDataRepository _userDataRepository;
    private readonly IUserInitializationDataRepository _userInitializationDataRepository;

    public TutorialService(IUserDataRepository userDataRepository, IUserInitializationDataRepository userInitializationDataRepository)
    {
        _userDataRepository = userDataRepository;
        _userInitializationDataRepository = userInitializationDataRepository;
    }

    public Task<bool> IsTutorialInProgress(ulong xuid) =>
        Task.FromResult(_usersInTutorial.Contains(xuid));

    public async Task UpdateTutorialStep(ulong xuid, uint step)
    {
        if (step < 130)
            _usersInTutorial.Add(xuid);
        else
            _usersInTutorial.Remove(xuid);

        await _userDataRepository.UpdateTutorialStep(xuid, step);
    }

    public async Task StartLotteryTutorial(ulong xuid, uint favoriteCharacterMasterId)
    {
        if (!await IsTutorialInProgress(xuid))
            return;

        await _userInitializationDataRepository.CreateForCharacter(xuid, favoriteCharacterMasterId);
    }

    public async Task ProgressLotteryTutorialWithDrawnCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId)
    {
        if (!await IsTutorialInProgress(xuid))
            return;

        await _userInitializationDataRepository.UpdateWithDrawedCard(xuid, favoriteCharacterMasterCardId, favoriteCharacterCardId);
    }

    public async Task FinishLotteryTutorial(ulong xuid)
    {
        if (!await IsTutorialInProgress(xuid))
            return;

        await _userInitializationDataRepository.Delete(xuid);
    }
}
