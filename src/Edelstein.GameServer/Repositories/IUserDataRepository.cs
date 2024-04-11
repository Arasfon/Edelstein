using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Repositories;

public interface IUserDataRepository
{
    public Task<UserData?> GetByXuid(ulong xuid);
    public Task UpdateTutorialStep(ulong xuid, uint step);
    public Task<UserData> SetStartingCard(ulong xuid, uint masterCardId, uint masterTitleId);
    public Task AddCardsAndCharacters(ulong xuid, IEnumerable<Card> cards, IEnumerable<Character> characters);
    public Task SetDeck(ulong xuid, byte slot, IEnumerable<ulong> mainCardIds);
    public Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled);

    public Task<List<Character>> GetDeckCharactersFromUserData(UserData? userData, uint deckSlot);
}
