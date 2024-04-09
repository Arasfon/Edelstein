namespace Edelstein.GameServer.Services;

public interface ITutorialService
{
    public Task<bool> IsTutorialInProgress(ulong xuid);

    public Task UpdateTutorialStep(ulong xuid, uint step);
    public Task StartLotteryTutorial(ulong xuid, uint favoriteCharacterMasterId);
    public Task ProgressLotteryTutorialWithDrawnCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId);
    public Task FinishLotteryTutorial(ulong xuid);
}
