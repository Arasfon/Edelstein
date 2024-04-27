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

    public Task AddGift(ulong xuid, Gift gift) =>
        throw new NotImplementedException();

    public Task AddGifts(ulong xuid, List<Gift> gift) =>
        throw new NotImplementedException();

    public Task AddGift(IClientSessionHandle session, ulong xuid, Gift gift) =>
        throw new NotImplementedException();

    public Task AddGifts(IClientSessionHandle session, ulong xuid, List<Gift> gift) =>
        throw new NotImplementedException();

    public Task ClaimGift(ulong xuid, Gift gift) =>
        throw new NotImplementedException();

    public Task ClaimAll(ulong xuid) =>
        throw new NotImplementedException();

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
}
