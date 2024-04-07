using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.GameServer.Repositories;

public class UserDataRepository : IUserDataRepository
{
    private readonly IMongoCollection<UserData> _userDataCollection;

    public UserDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>("user_data");
    }

    public async Task<UserData?> GetByXuid(ulong xuid) =>
        await _userDataCollection.Find(x => x.User.Id == xuid).FirstOrDefaultAsync();

    public async Task UpdateTutorialStep(ulong xuid, uint step)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update.Set(x => x.TutorialStep, step);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }
}
