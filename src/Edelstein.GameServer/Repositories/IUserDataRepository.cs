using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Repositories;

public interface IUserDataRepository
{
    public Task<UserData?> GetByXuid(ulong xuid);
    public Task UpdateTutorialStep(ulong xuid, uint step);
    public Task<UserData> SetStartingCard(ulong xuid, uint masterCardId, uint masterTitleId);
    public Task AddCards(ulong xuid, IEnumerable<Card> cards);
    public Task SetDeck(ulong xuid, byte slot, IEnumerable<ulong> mainCardIds);
    public Task AddCharacter(ulong xuid, uint masterCharacterId, uint experience = 1);
    public Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled);
}
