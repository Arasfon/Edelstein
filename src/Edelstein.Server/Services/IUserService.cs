using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Server.Models;

namespace Edelstein.Server.Services;

public interface IUserService
{
    public Task<UserRegistrationResult> RegisterUser(string publicKey);

    public Task<AuthenticationData?> GetAuthenticationDataByUserId(Guid userId);
    public Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid);

    public Task<UserData?> GetUserDataByXuid(ulong xuid);

    public Task<UserHomeDocument?> GetHomeByXuid(ulong xuid);

    public Task<User> InitializeUserStartingCharacterAndDeck(ulong xuid);

    public Task AddCards(ulong xuid, List<Card> cards, List<Card>? currentCards = null);
    public Task AddCharacter(ulong xuid, uint characterId, uint experience = 0);

    public Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled);

    public Task<List<Character>> GetDeckCharactersFromUserData(UserData? userData, uint deckSlot);
    public Task<List<Character>> GetDeckCharactersFromUserData(UserData userData, Deck deck);

    public Task<UserData> SetCardsItemsPointsCreatingIds(ulong xuid, List<Card> cards, List<Item> items, List<Point> points);

    public Task<UserData> SetGemsCardsItemsPointsLotteriesCreatingIds(ulong xuid, Gem gems, List<Card> cards, List<Item> items,
        List<Point> points, List<Lottery> lotteries);

    public Task UpdateUserLotteries(ulong xuid, List<Lottery> lotteries);

    public Task UpdateLastLoginTime(ulong xuid, long? timestamp = null);
    public Task<Gem?> ChargeGems(ulong xuid, int gemCharge);
    public Task<Deck> UpdateDeck(ulong xuid, byte slot, List<ulong> mainCardIds);
}
