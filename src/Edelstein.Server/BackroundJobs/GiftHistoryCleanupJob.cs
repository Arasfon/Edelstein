using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoAsyncEnumerableAdapter;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

using Quartz;

namespace Edelstein.Server.BackroundJobs;

public class GiftHistoryCleanupJob : IJob
{
    private readonly ILogger<GiftHistoryCleanupJob> _logger;
    private readonly IMongoCollection<Gift> _giftsCollection;

    public GiftHistoryCleanupJob(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient, ILogger<GiftHistoryCleanupJob> logger)
    {
        _logger = logger;
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _giftsCollection = mongoDatabase.GetCollection<Gift>(CollectionNames.Gifts);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting gift history cleanup job");

        DateTimeOffset currentDateTimeOffset = DateTimeOffset.UtcNow;

        FilterDefinition<Gift> oldReceivedFilterDefinition =
            Builders<Gift>.Filter.Lte(x => x.ReceivedDateTime, currentDateTimeOffset.AddDays(-30).ToUnixTimeSeconds()) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, true);

        _logger.LogInformation("Counting old received gifts to delete");

        long oldReceivedGiftsCount = await _giftsCollection.CountDocumentsAsync(oldReceivedFilterDefinition);

        if (oldReceivedGiftsCount > 0)
        {
            _logger.LogInformation("{Count} old received gifts will be deleted", oldReceivedGiftsCount);

            _logger.LogInformation("Starting old received gifts deletion");

            await _giftsCollection.DeleteManyAsync(oldReceivedFilterDefinition);

            _logger.LogInformation("Successfully deleted old received gifts");
        }
        else
            _logger.LogInformation("No old received gifts has been found");

        _logger.LogInformation("Starting excess gifts in history deletion");

        _logger.LogInformation("Executing excess gifts in history aggregation pipeline");

        // ReceiveId of ThresholdGift in GiftHistoryThreshold is the first gift history that should be deleted.
        // All gifts with ReceiveId less or equal to threshold's one are breaking 30 gift history limit and are excess.
        IAsyncEnumerable<GiftHistoryThreshold> giftsHistoryThresholds = (await _giftsCollection.Aggregate()
            .Match(Builders<Gift>.Filter.Eq(x => x.IsReceive, true))
            .Group(key => key.UserId,
                group => new HistoryGrouping(group.Key,
                    group.TopN(Builders<Gift>.Sort.Descending(x => x.ReceiveId), x => new ThresholdGift(x.ReceiveId), 31)))
            .Project(x => new GiftHistoryThreshold(x.UserId, x.History.ElementAtOrDefault(30)))
            .Match(x => x.ThresholdGift != null)
            .ToCursorAsync()).ToAsyncEnumerable();

        List<WriteModel<Gift>> deletions = new(1000);

        bool deletionsExist = false;

        _logger.LogInformation("Creating excess gifts in history deletion models");

        await foreach (GiftHistoryThreshold giftsHistoryThreshold in giftsHistoryThresholds)
        {
            FilterDefinition<Gift> filterDefinition =
                Builders<Gift>.Filter.Eq(x => x.UserId, giftsHistoryThreshold.UserId) &
                Builders<Gift>.Filter.Eq(x => x.IsReceive, true) &
                Builders<Gift>.Filter.Lte(x => x.ReceiveId, giftsHistoryThreshold.ThresholdGift!.ReceiveId);

            deletions.Add(new DeleteManyModel<Gift>(filterDefinition));

            deletionsExist = true;

            if (deletions.Count == 1000)
            {
                _logger.LogInformation("Starting excess gifts in history bulk (for {Count} users) deletion", deletions.Count);

                await _giftsCollection.BulkWriteAsync(deletions);
                deletions.Clear();

                _logger.LogInformation("Successfully bulk deleted excess gifts in history");
            }
        }

        if (deletions.Count > 0)
        {
            _logger.LogInformation("Starting excess gifts in history bulk (for {Count} users) deletion", deletions.Count);

            await _giftsCollection.BulkWriteAsync(deletions);

            _logger.LogInformation("Successfully bulk deleted excess gifts in history");
        }

        if (!deletionsExist)
            _logger.LogInformation("No excess gifts in history has been found");

        _logger.LogInformation("Completed gift history cleanup job");
    }

    private record ThresholdGift(ulong ReceiveId);

    private record HistoryGrouping(
        ulong UserId,
        IEnumerable<ThresholdGift> History
    );

    private record GiftHistoryThreshold(
        ulong UserId,
        ThresholdGift? ThresholdGift
    );
}
