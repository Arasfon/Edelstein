using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.PaymentServer.Repositories;

public class UserHomeRepository : IUserHomeRepository
{
    private readonly IMongoCollection<UserHomeDocument> _userHomeCollection;

    public UserHomeRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userHomeCollection = mongoDatabase.GetCollection<UserHomeDocument>("user_home");
    }

    public async Task Create(ulong xuid) =>
        await _userHomeCollection.InsertOneAsync(new UserHomeDocument
        {
            Xuid = xuid,
            Home = Home.CreateEmpty()
        });
}
