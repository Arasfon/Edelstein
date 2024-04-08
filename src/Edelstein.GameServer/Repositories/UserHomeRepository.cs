using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Edelstein.GameServer.Repositories;

public class UserHomeRepository : IUserHomeRepository
{
    private readonly IMongoCollection<UserHomeDocument> _userHomeCollection;

    public UserHomeRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userHomeCollection = mongoDatabase.GetCollection<UserHomeDocument>(CollectionNames.UserHome);
    }

    public async Task<UserHomeDocument?> GetByXuid(ulong xuid) =>
        await _userHomeCollection.Find(x => x.Xuid == xuid).FirstOrDefaultAsync();

    public async Task InitializePresets(ulong xuid, uint masterCardId)
    {
        FilterDefinition<UserHomeDocument> filterDefinition =
            Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid) &
            Builders<UserHomeDocument>.Filter.ElemMatch(x => x.Home.PresetSetting, x => x.Slot == 1);

        UpdateDefinition<UserHomeDocument> updateDefinition =
            Builders<UserHomeDocument>.Update.Set(x => x.Home.PresetSetting.FirstMatchingElement().IllustMasterCardId, masterCardId);

        await _userHomeCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }
}
