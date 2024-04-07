using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

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
}
