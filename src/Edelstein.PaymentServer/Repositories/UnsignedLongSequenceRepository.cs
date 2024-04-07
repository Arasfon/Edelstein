using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.PaymentServer.Repositories;

public class UnsignedLongSequenceRepository : ISequenceRepository<ulong>
{
    private readonly IMongoCollection<Sequence<ulong>> _sequencesCollection;

    public UnsignedLongSequenceRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _sequencesCollection = mongoDatabase.GetCollection<Sequence<ulong>>(CollectionNames.Sequences);
    }

    public async Task<ulong> GetNextValueById(string sequenceId, ulong initialValue = 0)
    {
        Sequence<ulong>? updatedDocument = await IncrementExistingSequence(sequenceId);

        if (updatedDocument is not null)
            return updatedDocument.CurrentValue;

        await _sequencesCollection.InsertOneAsync(new Sequence<ulong>
        {
            Id = sequenceId,
            CurrentValue = initialValue
        });

        updatedDocument = await IncrementExistingSequence(sequenceId);

        return updatedDocument!.CurrentValue;
    }

    private async Task<Sequence<ulong>?> IncrementExistingSequence(string sequenceId)
    {
        FilterDefinition<Sequence<ulong>> filter = Builders<Sequence<ulong>>.Filter.Eq(s => s.Id, sequenceId);
        UpdateDefinition<Sequence<ulong>> update = Builders<Sequence<ulong>>.Update.Inc(s => s.CurrentValue, 1ul);

        FindOneAndUpdateOptions<Sequence<ulong>> options = new() { ReturnDocument = ReturnDocument.After };

        Sequence<ulong>? updatedDocument = await _sequencesCollection.FindOneAndUpdateAsync(filter, update, options);

        return updatedDocument;
    }
}
