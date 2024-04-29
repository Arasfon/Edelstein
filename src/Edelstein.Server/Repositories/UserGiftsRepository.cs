using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.Server.Repositories;

public class UserGiftsRepository : IUserGiftsRepository
{
    private readonly IMongoCollection<Gift> _giftsCollection;

    public UserGiftsRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _giftsCollection = mongoDatabase.GetCollection<Gift>(CollectionNames.Gifts);
    }

    public async Task AddGift(Gift gift) =>
        await _giftsCollection.InsertOneAsync(gift);

    public async Task AddGifts(IEnumerable<Gift> gifts) =>
        await _giftsCollection.InsertManyAsync(gifts);

    public async Task MarkAsClaimed(ulong xuid, IEnumerable<ulong> giftIds, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // There is no efficient way to confirm that gift ids provided are within limit, so this allows to bypass gift limit until excess gifts are deleted
        // This is not a security risk as gift limit exists for efficiency, not for security
        FilterDefinition<Gift> filterDefinition =
            Builders<Gift>.Filter.In(x => x.Id, giftIds) &
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        UpdateDefinition<Gift> updateDefinition = Builders<Gift>.Update
            .Set(x => x.IsReceive, true)
            .Set(x => x.ReceivedDateTime, currentTimestamp);

        await _giftsCollection.UpdateManyAsync(filterDefinition, updateDefinition);
    }

    public async Task<IEnumerable<Gift>> GetAllByXuid(ulong xuid, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        FilterDefinition<Gift> nonClaimedFilterDefinition =
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        SortDefinition<Gift> nonClaimedSortDefinition = Builders<Gift>.Sort
            .Descending(x => x.CreatedDateTime)
            .Descending(x => x.Id);

        List<Gift> nonClaimedGifts =
            await _giftsCollection.Find(nonClaimedFilterDefinition).Sort(nonClaimedSortDefinition).Limit(100000).ToListAsync();

        FilterDefinition<Gift> claimHistoryFilterDefinition =
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, true);

        SortDefinition<Gift> claimHistorySortDefinition = Builders<Gift>.Sort
            .Descending(x => x.ReceivedDateTime)
            .Descending(x => x.Id);

        List<Gift> claimHistoryGifts =
            await _giftsCollection.Find(claimHistoryFilterDefinition).Sort(claimHistorySortDefinition).Limit(30).ToListAsync();

        return nonClaimedGifts.Concat(claimHistoryGifts);
    }

    public async Task<List<Gift>> GetManyByIds(IEnumerable<ulong> ids, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // There is no efficient way to confirm that gift ids provided are within limit, so this allows to bypass gift limit until excess gifts are deleted
        // This is not a security risk as gift limit exists for efficiency, not for security
        FilterDefinition<Gift> filterDefinition =
            Builders<Gift>.Filter.In(x => x.Id, ids) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        return await _giftsCollection.Find(filterDefinition).ToListAsync();
    }
}
