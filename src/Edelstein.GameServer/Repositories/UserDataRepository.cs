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
            .Set(x => x.User.MasterTitleIds, [masterTitleId]);

        FindOneAndUpdateOptions<UserData> options = new() { ReturnDocument = ReturnDocument.After };

        return await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition, options);
    }

    public async Task AddCardsAndCharacters(ulong xuid, IEnumerable<Card> cards, IEnumerable<Character> characters)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);
        UpdateDefinition<UserData> updateDefinition = Builders<UserData>.Update
            .AddToSetEach(x => x.CardList, cards)
            .AddToSetEach(x => x.CharacterList, characters);

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

    public async Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled)
    {
        FilterDefinition<UserData> filterDefinition = Builders<UserData>.Filter.Eq(x => x.User.Id, xuid);

        UpdateDefinitionBuilder<UserData> updateBuilder = Builders<UserData>.Update;
        List<UpdateDefinition<UserData>> updates = [];

        if (name is not null)
            updates.Add(updateBuilder.Set(x => x.User.Name, name));

        if (comment is not null)
            updates.Add(updateBuilder.Set(x => x.User.Comment, comment));

        if (favoriteMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.FavoriteMasterCardId, favoriteMasterCardId.Value));

        if (guestSmileMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.GuestSmileMasterCardId, guestSmileMasterCardId));

        if (guestPureMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.GuestPureMasterCardId, guestPureMasterCardId));

        if (guestCoolMasterCardId.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.GuestCoolMasterCardId, guestCoolMasterCardId));

        if (friendRequestDisabled.HasValue)
            updates.Add(updateBuilder.Set(x => x.User.FriendRequestDisabled, friendRequestDisabled));

        UpdateDefinition<UserData> updateDefinition = updateBuilder.Combine(updates);

        UserData userData = await _userDataCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition,
            new FindOneAndUpdateOptions<UserData> { ReturnDocument = ReturnDocument.After });

        return userData.User;
    }

    public Task<List<Character>> GetDeckCharactersFromUserData(UserData? userData, uint deckSlot)
    {
        IEnumerable<ulong> cardIds = userData!.DeckList[(int)deckSlot - 1].MainCardIds;

        Dictionary<ulong, uint> cardDict = userData.CardList.ToDictionary(x => x.Id, x => x.MasterCardId / 10000);
        Dictionary<uint, Character> characterDict = userData.CharacterList.ToDictionary(x => x.MasterCharacterId);

        List<Character> characters = cardIds
            .Select(cardId =>
            {
                cardDict.TryGetValue(cardId, out uint masterCharacterId);
                return masterCharacterId;
            })
            .Select(masterCharacterId =>
            {
                characterDict.TryGetValue(masterCharacterId, out Character? character);
                return character!;
            })
            .ToList();

        return Task.FromResult(characters);
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
