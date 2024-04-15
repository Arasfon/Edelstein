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

    public Task AddCards(ulong xuid, List<Card> cards, List<Card>? currentCards = null);
    public Task AddCharacter(ulong xuid, uint characterId, uint experience = 0);

    public Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled);

    public Task<UserData> SetCardsItemsPointsCreatingIds(ulong xuid, List<Card> cards, List<Item> items, List<Point> points);

    public Task<UserData> SetCardsItemsPointsLotteriesCreatingIds(ulong xuid, List<Card> cards, List<Item> items, List<Point> points,
        List<Lottery> lotteries);
}
