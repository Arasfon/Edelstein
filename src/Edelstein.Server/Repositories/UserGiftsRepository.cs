using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoAsyncEnumerableAdapter;

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

    public async Task AddGifts(IEnumerable<Gift> gifts) =>
        await _giftsCollection.InsertManyAsync(gifts);

    public async Task<long> CountForUser(ulong xuid, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        FilterDefinition<Gift> filterDefinition =
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        return await _giftsCollection.CountDocumentsAsync(filterDefinition);
    }

    public async Task DeleteOldestForUser(ulong xuid, int count, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        FilterDefinition<Gift> filterDefinition =
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        SortDefinition<Gift> sortDefinition = Builders<Gift>.Sort.Ascending(x => x.CreatedDateTime).Ascending(x => x.Id);

        List<ulong> giftIdsToDelete = await _giftsCollection.Find(filterDefinition).Sort(sortDefinition).Limit(count).Project(x => x.Id).ToListAsync();

        await _giftsCollection.DeleteManyAsync(Builders<Gift>.Filter.In(x => x.Id, giftIdsToDelete));
    }

    public async Task MarkAsClaimed(ulong xuid, IEnumerable<(ulong GiftId, ulong ReceiveId)> giftIdsWithReceiveIds, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        List<WriteModel<Gift>> updates = [];

        foreach ((ulong giftId, ulong receiveId) in giftIdsWithReceiveIds)
        {
            FilterDefinition<Gift> filterDefinition =
                Builders<Gift>.Filter.Eq(x => x.Id, giftId) &
                Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
                Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
                Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

            UpdateDefinition<Gift> updateDefinition = Builders<Gift>.Update
                .Set(x => x.IsReceive, true)
                .Set(x => x.ReceiveId, receiveId)
                .Set(x => x.ReceivedDateTime, currentTimestamp);

            updates.Add(new UpdateOneModel<Gift>(filterDefinition, updateDefinition));
        }

        await _giftsCollection.BulkWriteAsync(updates);
    }

    public async IAsyncEnumerable<Gift> GetAllByXuid(ulong xuid, long? currentTimestamp = null)
    {
        DateTimeOffset currentDateTimeOffset = currentTimestamp is not null ? DateTimeOffset.FromUnixTimeSeconds(currentTimestamp.Value) : DateTimeOffset.UtcNow;
        currentTimestamp ??= currentDateTimeOffset.ToUnixTimeSeconds();

        FilterDefinition<Gift> nonClaimedFilterDefinition =
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        SortDefinition<Gift> nonClaimedSortDefinition = Builders<Gift>.Sort
            .Descending(x => x.CreatedDateTime)
            .Descending(x => x.Id);

        IAsyncEnumerable<Gift> nonClaimedGifts =
            (await _giftsCollection.Find(nonClaimedFilterDefinition).Sort(nonClaimedSortDefinition).Limit(100000).ToCursorAsync().ConfigureAwait(false)).ToAsyncEnumerable();

        await foreach(Gift nonClaimedGift in nonClaimedGifts)
            yield return nonClaimedGift;

        FilterDefinition<Gift> claimHistoryFilterDefinition =
            Builders<Gift>.Filter.Eq(x => x.UserId, xuid) &
            Builders<Gift>.Filter.Gt(x => x.ReceivedDateTime, currentDateTimeOffset.AddDays(-30).ToUnixTimeSeconds()) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, true);

        SortDefinition<Gift> claimHistorySortDefinition = Builders<Gift>.Sort
            .Descending(x => x.ReceiveId);

        IAsyncEnumerable<Gift> claimHistoryGifts =
            (await _giftsCollection.Find(claimHistoryFilterDefinition).Sort(claimHistorySortDefinition).Limit(30).ToCursorAsync().ConfigureAwait(false)).ToAsyncEnumerable();

        await foreach(Gift claimedGift in claimHistoryGifts)
            yield return claimedGift;
    }

    public async Task<List<Gift>> GetManyByIds(IEnumerable<ulong> ids, long? currentTimestamp = null)
    {
        currentTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        FilterDefinition<Gift> filterDefinition =
            Builders<Gift>.Filter.In(x => x.Id, ids) &
            Builders<Gift>.Filter.Gt(x => x.ExpireDateTime, currentTimestamp.Value) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        return await _giftsCollection.Find(filterDefinition).ToListAsync();
    }
}
