using Edelstein.Data.Configuration;
using Edelstein.Data.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Edelstein.Server.Repositories;

public class AuthenticationDataRepository : IAuthenticationDataRepository
{
    private readonly IMongoCollection<AuthenticationData> _authenticationDataCollection;

    public AuthenticationDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _authenticationDataCollection = mongoDatabase.GetCollection<AuthenticationData>(CollectionNames.AuthenticationData);
    }

    public async Task<AuthenticationData> Create(ulong xuid, string publicKey)
    {
        AuthenticationData authenticationData = new(Guid.NewGuid(), xuid, publicKey);

        await _authenticationDataCollection.InsertOneAsync(authenticationData);

        return authenticationData;
    }

    public async Task<AuthenticationData?> GetByUserId(Guid userId) =>
        await _authenticationDataCollection.Find(x => x.UserId == userId).FirstOrDefaultAsync();

    public async Task<AuthenticationData?> GetByXuid(ulong xuid) =>
        await _authenticationDataCollection.Find(x => x.Xuid == xuid).FirstOrDefaultAsync();
}
