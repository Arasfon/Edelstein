using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.Server.Repositories;

public class UserInitializationDataRepository : IUserInitializationDataRepository
{
    private readonly IMongoCollection<UserInitializationData> _userInitializationDataCollection;

    public UserInitializationDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userInitializationDataCollection = mongoDatabase.GetCollection<UserInitializationData>(CollectionNames.UserInitialization);
    }

    public async Task<UserInitializationData> GetByXuid(ulong xuid) =>
        await _userInitializationDataCollection.Find(x => x.Xuid == xuid).FirstOrDefaultAsync();

    public async Task CreateForCharacter(ulong xuid, uint favoriteCharacterMasterId)
    {
        FilterDefinition<UserInitializationData> filterDefinition = Builders<UserInitializationData>.Filter.Eq(x => x.Xuid, xuid);
        UpdateDefinition<UserInitializationData> updateDefinition = Builders<UserInitializationData>.Update
            .Set(x => x.FavoriteCharacterMasterId, favoriteCharacterMasterId)
            .SetOnInsert(x => x.Xuid, xuid);

        await _userInitializationDataCollection.UpdateOneAsync(filterDefinition, updateDefinition, new UpdateOptions { IsUpsert = true });
    }

    public async Task UpdateWithDrawedCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId)
    {
        FilterDefinition<UserInitializationData> filterDefinition = Builders<UserInitializationData>.Filter.Eq(x => x.Xuid, xuid);
        UpdateDefinition<UserInitializationData> updateDefinition = Builders<UserInitializationData>.Update
            .Set(x => x.FavoriteCharacterMasterCardId, favoriteCharacterMasterCardId)
            .Set(x => x.FavoriteCharacterCardId, favoriteCharacterCardId);

        await _userInitializationDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task Delete(ulong xuid) =>
        await _userInitializationDataCollection.DeleteOneAsync(x => x.Xuid == xuid);
}
