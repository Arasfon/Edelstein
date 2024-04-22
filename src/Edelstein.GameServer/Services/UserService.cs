using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Repositories;
using Edelstein.GameServer.Extensions;
using Edelstein.GameServer.Models;
using Edelstein.GameServer.Repositories;

using System.Diagnostics;

namespace Edelstein.GameServer.Services;

public class UserService : IUserService
{
    private readonly IAuthenticationDataRepository _authenticationDataRepository;
    private readonly IUserDataRepository _userDataRepository;
    private readonly IUserHomeRepository _userHomeRepository;
    private readonly IUserMissionsRepository _userMissionsRepository;
    private readonly IUserInitializationDataRepository _userInitializationDataRepository;
    private readonly IDefaultGroupCardsFactoryService _defaultGroupCardsFactoryService;
    private readonly ITutorialService _tutorialService;
    private readonly ISequenceRepository<ulong> _sequenceRepository;

    public UserService(
        IAuthenticationDataRepository authenticationDataRepository,
        IUserDataRepository userDataRepository,
        IUserHomeRepository userHomeRepository,
        IUserMissionsRepository userMissionsRepository,
        IUserInitializationDataRepository userInitializationDataRepository,
        IDefaultGroupCardsFactoryService defaultGroupCardsFactoryService,
        ITutorialService tutorialService,
        ISequenceRepository<ulong> sequenceRepository)
    {
        _authenticationDataRepository = authenticationDataRepository;
        _userDataRepository = userDataRepository;
        _userHomeRepository = userHomeRepository;
        _userMissionsRepository = userMissionsRepository;
        _userInitializationDataRepository = userInitializationDataRepository;
        _defaultGroupCardsFactoryService = defaultGroupCardsFactoryService;
        _tutorialService = tutorialService;
        _sequenceRepository = sequenceRepository;
    }

    public async Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid) =>
        await _authenticationDataRepository.GetByXuid(xuid);

    public async Task<UserData?> GetUserDataByXuid(ulong xuid)
    {
        UserData? userData = await _userDataRepository.GetByXuid(xuid);

        if (userData?.TutorialStep < 130)
            await _tutorialService.MarkInTutorial(xuid);

        return userData;
    }

    public async Task<UserHomeDocument?> GetHomeByXuid(ulong xuid) =>
        await _userHomeRepository.GetByXuid(xuid);

    public async Task<UserMissionsDocument?> GetUserMissionsByXuid(ulong xuid) =>
        await _userMissionsRepository.GetUserMissionsByXuid(xuid);

    public async Task<User> InitializeUserStartingCharacterAndDeck(ulong xuid)
    {
        UserInitializationData userInitializationData = await _userInitializationDataRepository.GetByXuid(xuid);

        UserData updatedUserData = await InitializeStartingCardAndTitle(userInitializationData);
        await _userHomeRepository.InitializePresets(xuid, userInitializationData.FavoriteCharacterMasterCardId);
        await InitializeDeck(updatedUserData, userInitializationData);

        return updatedUserData.User;
    }

    private async Task<UserData> InitializeStartingCardAndTitle(UserInitializationData userInitializationData)
    {
        BandCategory group = (BandCategory)(userInitializationData.FavoriteCharacterMasterId / 1000);

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        uint masterTitleId = group switch
        {
            BandCategory.Muse => 0,
            BandCategory.Aqours => 9,
            BandCategory.Nijigaku => 9 + 9,
            BandCategory.Liella => 9 + 9 + 12,
            _ => throw new ArgumentException()
        };

        masterTitleId += 3000000 + userInitializationData.FavoriteCharacterMasterId % 100;

        return await _userDataRepository.SetStartingCard(userInitializationData.Xuid,
            userInitializationData.FavoriteCharacterMasterCardId, masterTitleId);
    }

    private async Task InitializeDeck(UserData currentUserData, UserInitializationData userInitializationData)
    {
        BandCategory group = (BandCategory)(userInitializationData.FavoriteCharacterMasterId / 1000);

        DefaultCardRetrievalResult defaultCardsRetrievalResult = await _defaultGroupCardsFactoryService.GetOrCreate(group, currentUserData.CardList);

        if (defaultCardsRetrievalResult.DuplicateCount > 0)
        {
            // Assuming all default characters are rare so their duplicates give 20 penlights
            const uint penlightItemId = 19100001;

            Item? penlightItem = currentUserData.ItemList.FirstOrDefault(x => x.MasterItemId == penlightItemId);

            if (penlightItem is null)
            {
                penlightItem = new Item
                {
                    Id = await _sequenceRepository.GetNextValueById(SequenceNames.ItemIds),
                    MasterItemId = penlightItemId,
                    Amount = 20 * defaultCardsRetrievalResult.DuplicateCount,
                    ExpireDateTime = null
                };
                currentUserData.ItemList.Add(penlightItem);
            }
            else
                penlightItem.Amount += 20 * defaultCardsRetrievalResult.DuplicateCount;
        }

        await AddCards(userInitializationData.Xuid, defaultCardsRetrievalResult.Cards, currentUserData.CardList);

        // Remove UR character duplicate
        List<ulong> cardIds = defaultCardsRetrievalResult.Cards.Where(x => x.MasterCardId / 10000 != userInitializationData.FavoriteCharacterMasterId)
            .Select(x => x.Id)
            .ToList();

        // Set center card
        cardIds.Insert(4, userInitializationData.FavoriteCharacterCardId);

        // Take only 9 cards
        await _userDataRepository.SetDeck(userInitializationData.Xuid, 1, cardIds.Take(9));
    }

    public async Task AddCards(ulong xuid, List<Card> cards, List<Card>? currentCards = null)
    {
        if (currentCards is null)
        {
            UserData? userData = await GetUserDataByXuid(xuid);
            Debug.Assert(userData is not null);

            currentCards = userData.CardList;
        }

        cards = cards.ExceptBy(currentCards, x => x.MasterCardId).ToList();

        await _userDataRepository.AddCards(xuid, cards);
    }

    public Task AddCharacter(ulong xuid, uint characterId, uint experience = 0) =>
        _userDataRepository.AddCharacter(xuid, characterId, experience);

    public async Task<User> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled) =>
        await _userDataRepository.UpdateUser(xuid, name, comment, favoriteMasterCardId, guestSmileMasterCardId,
            guestPureMasterCardId, guestCoolMasterCardId, friendRequestDisabled);

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
            .Select(masterCharacterId => characterDict.TryGetValue(masterCharacterId, out Character? character)
                ? character
                : new Character { MasterCharacterId = masterCharacterId })
            .ToList();

        return Task.FromResult(characters);
    }

    public async Task<UserData> SetCardsItemsPointsCreatingIds(ulong xuid, List<Card> cards, List<Item> items,
        List<Point> points)
    {
        List<Card> cardsWithoutIds = cards.Where(x => x.Id == 0).ToList();
        List<ulong> cardIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, (ulong)cardsWithoutIds.Count)).ToList();
        for (int i = 0; i < cardsWithoutIds.Count; i++)
            cardsWithoutIds[i].Id = cardIds[i];

        List<Item> itemsWithoutIds = items.Where(x => x.Id == 0).ToList();
        List<ulong> itemIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.ItemIds, (ulong)itemsWithoutIds.Count)).ToList();
        for (int i = 0; i < items.Count; i++)
            items[i].Id = itemIds[i];

        return await _userDataRepository.SetCardsItemsPoints(xuid, cards, items, points);
    }

    public async Task<UserData> SetGemsCardsItemsPointsLotteriesCreatingIds(ulong xuid, Gem gems, List<Card> cards, List<Item> items,
        List<Point> points, List<Lottery> lotteries)
    {
        List<Card> cardsWithoutIds = cards.Where(x => x.Id == 0).ToList();
        List<ulong> cardIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, (ulong)cardsWithoutIds.Count)).ToList();
        for (int i = 0; i < cardsWithoutIds.Count; i++)
            cardsWithoutIds[i].Id = cardIds[i];

        List<Item> itemsWithoutIds = items.Where(x => x.Id == 0).ToList();
        List<ulong> itemIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.ItemIds, (ulong)itemsWithoutIds.Count)).ToList();
        for (int i = 0; i < itemsWithoutIds.Count; i++)
            itemsWithoutIds[i].Id = itemIds[i];

        return await _userDataRepository.SetGemsCardsItemsPointsLotteries(xuid, gems, cards, items, points,
            lotteries);
    }
}
