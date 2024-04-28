using Edelstein.Data.Configuration;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.Server.Repositories;

public class LiveDataRepository : ILiveDataRepository
{
    private readonly IMongoCollection<UserData> _userDataCollection;
    private readonly IMongoCollection<Gift> _giftsCollection;

    public LiveDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>(CollectionNames.UserData);
        _giftsCollection = mongoDatabase.GetCollection<Gift>(CollectionNames.Gifts);
    }

    public async Task<UserData> UpdateAfterFinishedLive(ulong xuid, long currentTimestamp, List<Live> lives,
        LinkedList<Point> points, LinkedList<Item> items, Stamina stamina, int experience, Gem gem,
        List<Character> characters, List<LiveMission> liveMissions, HashSet<uint> newStampIds, LinkedList<Gift> gifts)
    {
        FilterDefinition<UserData> userDataFilter = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinitionBuilder<UserData> updateBuilder = Builders<UserData>.Update;
        List<UpdateDefinition<UserData>> updates = [];

        if(lives.Count > 0)
            updates.Add(updateBuilder.Set(x => x.LiveList, lives));

        if(points.Count > 0)
            updates.Add(updateBuilder.Set(x => x.PointList, points));

        if(items.Count > 0)
            updates.Add(updateBuilder.Set(x => x.ItemList, items));

        updates.Add(updateBuilder.Set(x => x.Stamina, stamina));

        updates.Add(updateBuilder.Set(x => x.User.Exp, experience));

        updates.Add(updateBuilder.Set(x => x.Gem, gem));

        if(characters.Count > 0)
            updates.Add(updateBuilder.Set(x => x.CharacterList, characters));

        if(liveMissions.Count > 0)
            updates.Add(updateBuilder.Set(x => x.LiveMissionList, liveMissions));

        updates.Add(updateBuilder.Set(x => x.User.LastLoginTime, currentTimestamp));

        if(newStampIds.Count > 0)
            updates.Add(updateBuilder.PushEach(x => x.MasterStampIds, newStampIds));

        UserData userData = await _userDataCollection.FindOneAndUpdateAsync(userDataFilter, updateBuilder.Combine(updates),
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });

        if (gifts.Count > 0)
            await _giftsCollection.InsertManyAsync(gifts);

        return userData;
    }
}
