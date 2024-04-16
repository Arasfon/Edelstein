namespace Edelstein.GameServer.Services;

public interface ITutorialService
{
    public Task<bool> IsTutorialInProgress(ulong xuid);

    public Task MarkInTutorial(ulong xuid);
    public Task UpdateTutorialStep(ulong xuid, uint step);
    public Task InitializeUserTutorialData(ulong xuid, uint favoriteCharacterMasterId);
    public Task ProgressTutorialWithDrawnCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId);
    public Task FinishTutorial(ulong xuid);
}
