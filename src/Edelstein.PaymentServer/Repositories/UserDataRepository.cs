using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.PaymentServer.Repositories;

public class UserDataRepository : IUserDataRepository
{
    private readonly IMongoCollection<UserData> _userDataCollection;

    public UserDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>("user_data");
    }

    public async Task<UserData> CreateTutorialUserData(ulong xuid)
    {
        UserData tutorialUserData = UserData.CreateTutorialUserData(xuid);

        await _userDataCollection.InsertOneAsync(tutorialUserData);

        return tutorialUserData;
    }

    public async Task<UserData?> GetByXuid(ulong xuid) =>
        await _userDataCollection.Find(x => x.User.Id == xuid).FirstOrDefaultAsync();
}
