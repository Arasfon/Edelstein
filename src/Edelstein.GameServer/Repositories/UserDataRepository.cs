using Edelstein.Data.Configuration;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using Microsoft.Extensions.Options;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Edelstein.GameServer.Repositories;

public class UserDataRepository : IUserDataRepository
{
    private readonly IMongoCollection<UserData> _userDataCollection;

    public UserDataRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userDataCollection = mongoDatabase.GetCollection<UserData>(CollectionNames.UserData);
    }

    public async Task<UserData?> GetByXuid(ulong xuid) =>
        await _userDataCollection.Find(x => x.User.Id == xuid).FirstOrDefaultAsync();

    public async Task UpdateTutorialStep(ulong xuid, uint step)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update.Set(x => x.TutorialStep, step);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task<UserData> SetStartingCard(ulong xuid, uint masterCardId, uint masterTitleId)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .Set(x => x.User.FavoriteMasterCardId, masterCardId)
            .Set(x => x.User.GuestSmileMasterCardId, masterCardId)
            .Set(x => x.User.GuestCoolMasterCardId, masterCardId)
            .Set(x => x.User.GuestPureMasterCardId, masterCardId)
            .Set(x => x.User.MasterTitleIds, [masterTitleId, 0]);

        FindOneAndUpdateOptions<UserData> options = new() { ReturnDocument = ReturnDocument.After };

        return await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition, options);
    }

    public async Task AddCards(ulong xuid, IEnumerable<Card> cards)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update.AddToSetEach(x => x.CardList, cards);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task SetDeck(ulong xuid, byte slot, IEnumerable<ulong> mainCardIds)
    {
        FilterDefinition<UserData> filterDefinition =
            Builders<UserData>.Filter.Eq(x => x.User.Id, xuid) &
            Builders<UserData>.Filter.ElemMatch(x => x.DeckList, x => x.Slot == slot);

        UpdateDefinition<UserData> updateDefinition =
            Builders<UserData>.Update.Set(x => x.DeckList.FirstMatchingElement().MainCardIds, mainCardIds);

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task AddCharacter(ulong xuid, uint masterCharacterId, uint experience = 1)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update.AddToSet(x => x.CharacterList,
            new Character
            {
                MasterCharacterId = masterCharacterId,
                Exp = experience,
                BeforeExp = 1
            });

        await _userDataCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }
}
