using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Server.Builders;
using Edelstein.Server.Extensions;
using Edelstein.Server.Models;
using Edelstein.Server.Repositories;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
using System.Globalization;

using BandCategory = Edelstein.Data.Models.Components.BandCategory;

namespace Edelstein.Server.Services;

public class UserService : IUserService
{
    private readonly IAuthenticationDataRepository _authenticationDataRepository;
    private readonly IUserDataRepository _userDataRepository;
    private readonly IUserHomeRepository _userHomeRepository;
    private readonly IUserInitializationDataRepository _userInitializationDataRepository;
    private readonly IDefaultGroupCardsFactoryService _defaultGroupCardsFactoryService;
    private readonly ITutorialService _tutorialService;
    private readonly ISequenceRepository<ulong> _sequenceRepository;
    private readonly IUserGiftsRepository _userGiftsRepository;
    private readonly MstDbContext _mstDbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IAuthenticationDataRepository authenticationDataRepository,
        IUserDataRepository userDataRepository,
        IUserHomeRepository userHomeRepository,
        IUserInitializationDataRepository userInitializationDataRepository,
        IDefaultGroupCardsFactoryService defaultGroupCardsFactoryService,
        ITutorialService tutorialService,
        ISequenceRepository<ulong> sequenceRepository,
        IUserGiftsRepository userGiftsRepository,
        MstDbContext mstDbContext,
        ILogger<UserService> logger)
    {
        _authenticationDataRepository = authenticationDataRepository;
        _userDataRepository = userDataRepository;
        _userHomeRepository = userHomeRepository;
        _userInitializationDataRepository = userInitializationDataRepository;
        _defaultGroupCardsFactoryService = defaultGroupCardsFactoryService;
        _tutorialService = tutorialService;
        _sequenceRepository = sequenceRepository;
        _userGiftsRepository = userGiftsRepository;
        _mstDbContext = mstDbContext;
        _logger = logger;
    }

    public async Task<UserRegistrationResult> RegisterUser(string publicKey)
    {
        AuthenticationData authenticationData = await _authenticationDataRepository.Create(await GetNextXuid(), publicKey);
        UserData userData = await _userDataRepository.CreateTutorialUserData(authenticationData.Xuid);
        await _userHomeRepository.Create(authenticationData.Xuid);

        return new UserRegistrationResult(authenticationData, userData);
    }

    public async Task<AuthenticationData?> GetAuthenticationDataByUserId(Guid userId) =>
        await _authenticationDataRepository.GetByUserId(userId);

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

        DefaultCardRetrievalResult defaultCardsRetrievalResult =
            await _defaultGroupCardsFactoryService.GetOrCreate(group, currentUserData.CardList);

        if (defaultCardsRetrievalResult.DuplicateCount > 0)
        {
            // Assuming all default characters are rare so their duplicates give 20 penlights
            const uint penlightItemId = 19100001;

            ResourcesModificationResult resourcesModificationResult =
                new ResourceAdditionBuilder(currentUserData)
                    .AddItem(penlightItemId, 20 * defaultCardsRetrievalResult.DuplicateCount)
                    .Chain()
                    .Build();

            await UpdateResources(currentUserData.User.Id, resourcesModificationResult);
        }

        await AddCards(userInitializationData.Xuid, defaultCardsRetrievalResult.Cards, currentUserData.CardList);

        // Remove UR character duplicate
        List<ulong> cardIds = defaultCardsRetrievalResult.Cards
            .Where(x => x.MasterCardId / 10000 != userInitializationData.FavoriteCharacterMasterId)
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

    public async Task<UserData> UpdateResources(ulong xuid, ResourcesModificationResult resourcesModificationResult)
    {
        if (resourcesModificationResult.Gifts!.Count > 0)
            await AddGifts(xuid, resourcesModificationResult.Gifts);

        List<Card> cardsWithoutIds = resourcesModificationResult.Updates.CardList.Where(x => x.Id == 0).ToList();
        List<ulong> cardIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, (ulong)cardsWithoutIds.Count)).ToList();
        for (int i = 0; i < cardsWithoutIds.Count; i++)
            cardsWithoutIds[i].Id = cardIds[i];

        List<Item> itemsWithoutIds = resourcesModificationResult.Updates.ItemList.Where(x => x.Id == 0).ToList();
        List<ulong> itemIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.ItemIds, (ulong)itemsWithoutIds.Count)).ToList();
        for (int i = 0; i < itemsWithoutIds.Count; i++)
            itemsWithoutIds[i].Id = itemIds[i];

        return await _userDataRepository.UpdateUser(xuid, resourcesModificationResult);
    }

    public async Task<List<Character>> GetDeckCharactersFromUserData(UserData? userData, uint deckSlot) =>
        await GetDeckCharactersFromUserData(userData!, userData!.DeckList.First(x => x.Slot == deckSlot));

    public Task<List<Character>> GetDeckCharactersFromUserData(UserData userData, Deck deck)
    {
        List<ulong> cardIds = deck.MainCardIds;

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
        IEnumerable<Point> points)
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

    public async Task<UserData> SetGemsCardsItemsPointsLotteriesCreatingIds(ulong xuid, Gem gem, List<Card> cards,
        List<Item> items, IEnumerable<Point> points, IEnumerable<Lottery> lotteries)
    {
        List<Card> cardsWithoutIds = cards.Where(x => x.Id == 0).ToList();
        List<ulong> cardIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, (ulong)cardsWithoutIds.Count)).ToList();
        for (int i = 0; i < cardsWithoutIds.Count; i++)
            cardsWithoutIds[i].Id = cardIds[i];

        List<Item> itemsWithoutIds = items.Where(x => x.Id == 0).ToList();
        List<ulong> itemIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.ItemIds, (ulong)itemsWithoutIds.Count)).ToList();
        for (int i = 0; i < itemsWithoutIds.Count; i++)
            itemsWithoutIds[i].Id = itemIds[i];

        return await _userDataRepository.SetGemsCardsItemsPointsLotteries(xuid, gem, cards, items, points,
            lotteries);
    }

    public async Task UpdateUserLotteries(ulong xuid, List<Lottery> lotteries) =>
        await _userDataRepository.UpdateUserLotteries(xuid, lotteries);

    public async Task UpdateLastLoginTime(ulong xuid, long? timestamp = null)
    {
        timestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _userDataRepository.UpdateLastLoginTime(xuid, timestamp.Value);
    }

    public async Task<Gem?> ChargeGems(ulong xuid, int gemCharge)
    {
        UserData userData = (await _userDataRepository.GetByXuid(xuid))!;

        (bool enoughGems, int freeCharge, int paidCharge) = ResourceConsumptionBuilder.TryDistributeConsumeGems(userData.Gem, gemCharge);

        if (!enoughGems)
            return null;

        return await _userDataRepository.IncrementGems(xuid, -freeCharge, -paidCharge);
    }

    public async Task<Deck> UpdateDeck(ulong xuid, byte slot, List<ulong> mainCardIds)
    {
        List<ulong> nonZeroes = mainCardIds.Where(x => x != 0).ToList();
        if (mainCardIds.Count != 9 || nonZeroes.Count != nonZeroes.Distinct().Count())
            throw new Exception("Invalid card ids");

        return await _userDataRepository.SetDeck(xuid, slot, mainCardIds);
    }

    private async Task<ulong> GetNextXuid() =>
        await _sequenceRepository.GetNextValueById(SequenceNames.Xuids, 10000_00000_00000);

    public IAsyncEnumerable<Gift> GetAllGifts(ulong xuid) =>
        _userGiftsRepository.GetAllByXuid(xuid);

    public async Task AddGifts(ulong xuid, List<Gift> gifts)
    {
        if (gifts.Count == 0)
            return;

        // TODO: Check for gifts.Count and reduce them to 100000 last

        List<Gift> giftsWithoutIds = gifts.Where(x => x.Id == 0).ToList();
        List<ulong> giftIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.GiftIds, (ulong)giftsWithoutIds.Count)).ToList();
        for (int i = 0; i < giftsWithoutIds.Count; i++)
        {
            giftsWithoutIds[i].UserId = xuid;
            giftsWithoutIds[i].Id = giftIds[i];
        }

        int currentGifts = (int)await _userGiftsRepository.CountForUser(xuid);

        if (currentGifts + gifts.Count > 100000)
        {
            Task oldestDeletionTask = _userGiftsRepository.DeleteOldestForUser(xuid, currentGifts + gifts.Count - 100000);

            _ = oldestDeletionTask.ContinueWith(x =>
            {
                if (x.IsFaulted)
                    _logger.LogError(x.Exception, "Something wrong happened while deleting oldest gifts for user {Xuid}", xuid);
            });
        }

        await _userGiftsRepository.AddGifts(gifts);
    }

    public async Task<GiftClaimResult> ClaimGifts(ulong xuid, HashSet<ulong> giftIds)
    {
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        List<ulong> failedGiftIds = [];

        List<Gift> gifts = await _userGiftsRepository.GetManyByIds(giftIds, currentTimestamp);

        if (gifts.Count == 0)
            return new GiftClaimResult([], new UpdatedValueList(), []);

        UserData userData = (await GetUserDataByXuid(xuid))!;

        ResourceAdditionBuilder resourceAdditionBuilder = new(userData, currentTimestamp, true);

        IEnumerable<ulong> receiveIds = await _sequenceRepository.GetNextRangeById(SequenceNames.GiftReceiveIds, (ulong)gifts.Count);

        List<uint> cardGiftsCardIds = gifts.Where(x => x.RewardType == RewardType.Card)
            .Select(x => x.Value)
            .ToList();
        Dictionary<uint, CardMst> cardMsts =
            await _mstDbContext.CardMsts.Where(x => cardGiftsCardIds.Contains(x.Id))
                .Distinct()
                .ToDictionaryAsync(x => x.Id);

        List<uint> itemGiftsItemIds = gifts.Where(x => x.RewardType == RewardType.Item)
            .Select(x => x.Value)
            .ToList();
        Dictionary<uint, ItemMst> itemMsts =
            await _mstDbContext.ItemMsts.Where(x => itemGiftsItemIds.Contains(x.Id))
                .Distinct()
                .ToDictionaryAsync(x => x.Id);

        foreach (Gift gift in gifts)
        {
            ResourceConfigurer resourceConfigurer;

            switch (gift.RewardType)
            {
                case RewardType.Card:
                {
                    resourceConfigurer = resourceAdditionBuilder.ClaimGift(gift, cardMsts[gift.Value].Rarity);
                    break;
                }
                case RewardType.Item:
                {
                    string expireDateString = itemMsts[gift.Value].ExpireDate;
                    long? expirationTimestamp = null;

                    if (!String.IsNullOrEmpty(expireDateString))
                    {
                        expirationTimestamp = DateTimeOffset.ParseExact(expireDateString, "yyyy-MM-dd HH:mm:ss",
                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                            .ToUnixTimeSeconds();
                    }

                    resourceConfigurer = resourceAdditionBuilder.ClaimGift(gift, itemExpirationTimestamp: expirationTimestamp);
                    break;
                }
                default:
                {
                    resourceConfigurer = resourceAdditionBuilder.ClaimGift(gift);
                    break;
                }
            }

            resourceConfigurer = resourceConfigurer.SetGiveType(GiveType.Direct);

            if (resourceConfigurer is not { IsAdded: true, IsResourceConvertedToGift: false })
            {
                failedGiftIds.Add(gift.Id);
                giftIds.Remove(gift.Id);
            }
        }

        await _userGiftsRepository.MarkAsClaimed(xuid, giftIds.Zip(receiveIds), currentTimestamp);

        ResourcesModificationResult resourcesModificationResult = resourceAdditionBuilder.Build();

        await UpdateResources(xuid, resourcesModificationResult);

        return new GiftClaimResult(failedGiftIds, resourcesModificationResult.Updates, resourcesModificationResult.Rewards ?? []);
    }
}
