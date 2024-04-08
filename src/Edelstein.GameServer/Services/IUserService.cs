using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Services;

public interface IUserService
{
    public Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid);
    public Task<UserData?> GetUserDataByXuid(ulong xuid);

    public Task<UserHomeDocument?> GetHomeByXuid(ulong xuid);
    public Task<UserMissionsDocument?> GetUserMissionsByXuid(ulong xuid);

    public Task ProgressTutorial(ulong userXuid, uint step);
    public Task<User> InitializeUserStartingCharacterAndDeck(ulong xuid);
    public Task InitializeLotteryTutorial(ulong xuid, uint favoriteCharacterMasterId);
    public Task UpdateLotteryTutorialWithDrawnCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId);
    public Task CompleteLotteryTutorial(ulong xuid);

    public Task AddCardsToUser(ulong xuid, IEnumerable<Card> cards);
    public Task AddCharacter(ulong xuid, uint masterCharacterId);
}
