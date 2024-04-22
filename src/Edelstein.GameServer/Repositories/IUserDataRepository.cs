using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Repositories;

public interface IUserDataRepository
{
    public Task<UserData?> GetByXuid(ulong xuid);
    public Task UpdateTutorialStep(ulong xuid, uint step);
    public Task<UserData> SetStartingCard(ulong xuid, uint masterCardId, uint masterTitleId);
    public Task AddCards(ulong xuid, List<Card> cards);
    public Task AddCharacter(ulong xuid, uint characterId, uint experience = 0);
    public Task SetDeck(ulong xuid, byte slot, IEnumerable<ulong> mainCardIds);

    public Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled);

    public Task<UserData> SetCardsItemsPoints(ulong xuid, IEnumerable<Card> cards, IEnumerable<Item> items, IEnumerable<Point> points);

    public Task<UserData> SetGemsCardsItemsPointsLotteries(ulong xuid, Gem gems, List<Card> cards, List<Item> items, List<Point> points,
        List<Lottery> lotteries);

    public Task SetCurrentLiveData(ulong xuid, CurrentLiveData? currentLiveData);
    public Task<UserData> GetByXuidRemovingCurrentLiveData(ulong xuid);
}
