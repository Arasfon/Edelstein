using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Services;

public interface IUserService
{
    public Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid);
    public Task<UserData?> GetUserDataByXuid(ulong xuid);

    public Task<UserHomeDocument?> GetHomeByXuid(ulong xuid);
    public Task<UserMissionsDocument?> GetUserMissionsByXuid(ulong xuid);

    public Task<User> InitializeUserStartingCharacterAndDeck(ulong xuid);

    public Task AddCardsToUser(ulong xuid, IEnumerable<Card> cards);
    public Task AddCharacter(ulong xuid, uint masterCharacterId);

    public Task<User?> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled);
}
