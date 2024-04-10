using Edelstein.GameServer.Repositories;

using System.Collections.Concurrent;

namespace Edelstein.GameServer.Services;

public class TutorialService : ITutorialService
{
    private static readonly ConcurrentDictionary<ulong, byte> UsersInTutorial = [];

    private readonly IUserDataRepository _userDataRepository;
    private readonly IUserInitializationDataRepository _userInitializationDataRepository;

    public TutorialService(IUserDataRepository userDataRepository, IUserInitializationDataRepository userInitializationDataRepository)
    {
        _userDataRepository = userDataRepository;
        _userInitializationDataRepository = userInitializationDataRepository;
    }

    public Task<bool> IsTutorialInProgress(ulong xuid) =>
        Task.FromResult(UsersInTutorial.ContainsKey(xuid));

    public async Task UpdateTutorialStep(ulong xuid, uint step)
    {
        if (step < 130)
            UsersInTutorial.TryAdd(xuid, 0);
        else
            UsersInTutorial.TryRemove(xuid, out _);

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
