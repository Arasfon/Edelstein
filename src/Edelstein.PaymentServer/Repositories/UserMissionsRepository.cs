using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.PaymentServer.Repositories;

public class UserMissionsRepository : IUserMissionsRepository
{
    private readonly IMongoCollection<UserMissionsDocument> _userMissionsCollection;

    public UserMissionsRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userMissionsCollection = mongoDatabase.GetCollection<UserMissionsDocument>(CollectionNames.UserMissions);
    }

    public async Task Create(ulong xuid) =>
        await _userMissionsCollection.InsertOneAsync(new UserMissionsDocument
        {
            Xuid = xuid
        });
}
