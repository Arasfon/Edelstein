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

    public async Task<UserData> UpdateAfterFinishedLive(ulong xuid, long currentTimestamp, List<Live> lives, List<Point> points,
        List<Item> items, Stamina stamina, int experience, Gem gem, List<Character> characters,
        List<LiveMission> liveMissions, List<uint> stampIds, List<Gift> gifts, List<uint> clearedMissionIds)
    {
        // TODO: Transaction
        // TODO: Reconsider clearedMissionIds

        FilterDefinition<UserData> userDataFilter = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> userDataUpdate = Builders<UserData>.Update
            .Set(x => x.LiveList, lives)
            .Set(x => x.PointList, points)
            .Set(x => x.ItemList, items)
            .Set(x => x.Stamina, stamina)
            .Set(x => x.User.Exp, experience)
            .Set(x => x.Gem, gem)
            .Set(x => x.CharacterList, characters)
            .Set(x => x.LiveMissionList, liveMissions)
            .Set(x => x.User.LastLoginTime, currentTimestamp)
            .Set(x => x.MasterStampIds, stampIds);

        UserData userData = await _userDataCollection.FindOneAndUpdateAsync(userDataFilter, userDataUpdate,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });

        FilterDefinition<UserHomeDocument> homeDataFilter = Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid);
        UpdateDefinition<UserHomeDocument> homeDataUpdate = Builders<UserHomeDocument>.Update
            // TODO: Full replace to check for expired/old?
            .PushEach(x => x.Home.GiftList, gifts);

        await _userHomeCollection.UpdateOneAsync(homeDataFilter, homeDataUpdate);

        return userData;
    }
}
