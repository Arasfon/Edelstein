using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

using Quartz;

namespace Edelstein.Server.BackroundJobs;

public class ExpiredGiftsCleanupJob : IJob
{
    private readonly ILogger<ExpiredGiftsCleanupJob> _logger;
    private readonly IMongoCollection<Gift> _giftsCollection;

    public ExpiredGiftsCleanupJob(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient,
        ILogger<ExpiredGiftsCleanupJob> logger)
    {
        _logger = logger;
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _giftsCollection = mongoDatabase.GetCollection<Gift>(CollectionNames.Gifts);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting expired gifts cleanup job");

        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        FilterDefinition<Gift> filterDefinition =
            Builders<Gift>.Filter.Lte(x => x.ExpireDateTime, currentTimestamp) &
            Builders<Gift>.Filter.Eq(x => x.IsReceive, false);

        _logger.LogInformation("Counting expired gifts to delete");

        long count = await _giftsCollection.CountDocumentsAsync(filterDefinition);

        if (count > 0)
        {
            _logger.LogInformation("{Count} expired gifts will be deleted", count);

            _logger.LogInformation("Starting expired gifts deletion");

            await _giftsCollection.DeleteManyAsync(filterDefinition);

            _logger.LogInformation("Successfully deleted expired gifts");
        }
        else
            _logger.LogInformation("No expired gifts has been found");

        _logger.LogInformation("Completed expired gifts cleanup job");
    }
}
