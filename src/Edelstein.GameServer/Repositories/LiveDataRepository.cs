using Edelstein.Data.Configuration;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.GameServer.Repositories;

public class LiveDataRepository : ILiveDataRepository
{
    private readonly IMongoCollection<UserData> _userDataCollection;
    private readonly IMongoCollection<UserHomeDocument> _userHomeCollection;

    public LiveDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>(CollectionNames.UserData);
        _userHomeCollection = mongoDatabase.GetCollection<UserHomeDocument>(CollectionNames.UserHome);
    }

    public async Task<UserData> UpdateAfterLive(ulong xuid, Live updatedLive, List<Point> updatedPoints, List<Item> updatedItems,
        int staminaChange, int experienceChange, int gemChange, List<Character> updatedCharacters, List<Gift> newGifts,
        LiveMission liveMission, List<uint> clearedMissionIds, List<EventPoint> eventPointUpdates, List<Reward> eventPointRewards,
        RankingChange? rankingChange = null, EventMember? eventMember = null, EventRankingData? eventRankingData = null)
    {
        // TODO: Transaction

        // TODO: Event data

        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        FilterDefinition<UserData> userDataFilter = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> userDataUpdate = Builders<UserData>.Update
            // Coins
            .Set(x => x.PointList, updatedPoints)
            // Stamina
            .Inc(x => x.Stamina.StaminaValue, staminaChange)
            .Set(x => x.Stamina.LastUpdatedTime, currentTimestamp)
            // Experience
            .Inc(x => x.User.Exp, experienceChange)
            // Gems
            .Inc(x => x.Gem.Free, gemChange)
            .Inc(x => x.Gem.Total, gemChange)
            // Live
            .Push(x => x.LiveList, updatedLive)
            // Characters
            .Set(x => x.CharacterList, updatedCharacters)
            // Items
            .PushEach(x => x.ItemList, updatedItems)
            // Live missions
            .Push(x => x.LiveMissionList, liveMission);

        UserData updatedUserData = await _userDataCollection.FindOneAndUpdateAsync(userDataFilter, userDataUpdate,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });

        FilterDefinition<UserHomeDocument> homeDataFilter = Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid);
        UpdateDefinition<UserHomeDocument> homeDataUpdate = Builders<UserHomeDocument>.Update
            // Missions
            .PushEach(x => x.ClearMissionIds, clearedMissionIds)
            // Gifts
            .PushEach(x => x.Home.GiftList, newGifts);

        await _userHomeCollection.UpdateOneAsync(homeDataFilter, homeDataUpdate);

        return updatedUserData;
    }
}
