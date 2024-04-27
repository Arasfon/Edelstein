using Edelstein.Data.Configuration;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.Server.Repositories;

public class LiveDataRepository : ILiveDataRepository
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoCollection<UserData> _userDataCollection;
    private readonly IMongoCollection<Gift> _giftsCollection;

    public LiveDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
        IMongoDatabase mongoDatabase = _mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>(CollectionNames.UserData);
        _giftsCollection = mongoDatabase.GetCollection<Gift>(CollectionNames.Gifts);
    }

    public async Task<UserData> UpdateAfterFinishedLive(ulong xuid, long currentTimestamp, List<Live> lives, List<Point> points,
        List<Item> items, Stamina stamina, int experience, Gem gem, List<Character> characters,
        List<LiveMission> liveMissions, List<uint> stampIds, List<Gift> gifts, List<uint> clearedMissionIds)
    {
        using IClientSessionHandle session = await _mongoClient.StartSessionAsync();

        session.StartTransaction();

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

        UserData userData = await _userDataCollection.FindOneAndUpdateAsync(session, userDataFilter, userDataUpdate,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });

        await _giftsCollection.InsertManyAsync(session, gifts);

        await session.CommitTransactionAsync();

        return userData;
    }
}
