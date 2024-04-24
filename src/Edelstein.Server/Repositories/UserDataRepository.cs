using Edelstein.Data.Configuration;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using Microsoft.Extensions.Options;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Edelstein.Server.Repositories;

public class UserDataRepository : IUserDataRepository
{
    private readonly IMongoCollection<UserData> _userDataCollection;

    public UserDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>(CollectionNames.UserData);
    }

    public async Task<UserData> CreateTutorialUserData(ulong xuid)
    {
        UserData tutorialUserData = UserData.CreateTutorialUserData(xuid);

        await _userDataCollection.InsertOneAsync(tutorialUserData);

        return tutorialUserData;
    }

    public async Task<UserData?> GetByXuid(ulong xuid) =>
        await _userDataCollection.Find(x => x.User.Id == xuid).FirstOrDefaultAsync();

    public async Task UpdateTutorialStep(ulong xuid, uint step)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update.Set(x => x.TutorialStep, step);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task<UserData> SetStartingCard(ulong xuid, uint masterCardId, uint masterTitleId)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.User.FavoriteMasterCardId, masterCardId)
            .Set(x => x.User.GuestSmileMasterCardId, masterCardId)
            .Set(x => x.User.GuestCoolMasterCardId, masterCardId)
            .Set(x => x.User.GuestPureMasterCardId, masterCardId)
            .Set(x => x.User.MasterTitleIds, [masterTitleId, 0])
            .AddToSet(x => x.MasterTitleIds, masterTitleId);

        FindOneAndUpdateOptions<UserData> options = new() { ReturnDocument = ReturnDocument.After };

        return await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition, options);
    }

    public async Task AddCards(ulong xuid, List<Card> cards)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .AddToSetEach(x => x.CardList, cards);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task AddCharacter(ulong xuid, uint masterCharacterId, uint experience = 1)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update.AddToSet(x => x.CharacterList,
            new Character
            {
                MasterCharacterId = masterCharacterId,
                Exp = experience,
                BeforeExp = experience
            });

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task SetDeck(ulong xuid, byte slot, IEnumerable<ulong> mainCardIds)
    {
        FilterDefinition<UserData> filterDefinition =
            Builders<UserData>.Filter.Eq(x => x.User.Id, xuid) &
            Builders<UserData>.Filter.ElemMatch(x => x.DeckList, x => x.Slot == slot);

        UpdateDefinition<UserData> updateDefinition =
            Builders<UserData>.Update.Set(x => x.DeckList.FirstMatchingElement().MainCardIds, mainCardIds);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinitionBuilder<UserData> updateBuilder = Builders<UserData>.Update;
        List<UpdateDefinition<UserData>> updates = [];

        if (name is not null)
            updates.Add(updateBuilder.Set(x => x.User.Name, name));

        if (comment is not null)
            updates.Add(updateBuilder.Set(x => x.User.Comment, comment));

        if (favoriteMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.FavoriteMasterCardId, favoriteMasterCardId.Value));

        if (guestSmileMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.GuestSmileMasterCardId, guestSmileMasterCardId));

        if (guestPureMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.GuestPureMasterCardId, guestPureMasterCardId));

        if (guestCoolMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.GuestCoolMasterCardId, guestCoolMasterCardId));

        if (friendRequestDisabled.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.FriendRequestDisabled, friendRequestDisabled));

        UpdateDefinition<UserData> updateDefinition = updateBuilder.Combine(updates);

        UserData userData = await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });

        return userData.User;
    }

    public async Task<UserData> SetCardsItemsPoints(ulong xuid, IEnumerable<Card> cards, IEnumerable<Item> items, IEnumerable<Point> points)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.CardList, cards)
            .Set(x => x.ItemList, items)
            .Set(x => x.PointList, points);

        return await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });
    }

    public async Task<UserData> SetGemsCardsItemsPointsLotteries(ulong xuid, Gem gems, List<Card> cards, List<Item> items,
        List<Point> points, List<Lottery> lotteries)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.Gem, gems)
            .Set(x => x.CardList, cards)
            .Set(x => x.ItemList, items)
            .Set(x => x.PointList, points)
            .Set(x => x.LotteryList, lotteries);

        return await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });
    }

    public async Task SetCurrentLiveData(ulong xuid, CurrentLiveData? currentLiveData)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.CurrentLive, currentLiveData);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task<UserData> GetByXuidRemovingCurrentLiveData(ulong xuid)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.CurrentLive, null);

        return await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.Before });
    }

    public async Task SetStartTime(ulong xuid, long timestamp)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.StartTime, timestamp);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task UpdateUserLotteries(ulong xuid, List<Lottery> lotteries)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.LotteryList, lotteries);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task UpdateLastLoginTime(ulong xuid, long timestamp)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.User.LastLoginTime, timestamp);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }
}
