using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.GameServer.Extensions;
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

    public UserService(
        IAuthenticationDataRepository authenticationDataRepository,
        IUserDataRepository userDataRepository,
        IUserHomeRepository userHomeRepository,
        IUserMissionsRepository userMissionsRepository,
        IUserInitializationDataRepository userInitializationDataRepository,
        IDefaultGroupCardsFactoryService defaultGroupCardsFactoryService,
        ITutorialService tutorialService)
    {
        _authenticationDataRepository = authenticationDataRepository;
        _userDataRepository = userDataRepository;
        _userHomeRepository = userHomeRepository;
        _userMissionsRepository = userMissionsRepository;
        _userInitializationDataRepository = userInitializationDataRepository;
        _defaultGroupCardsFactoryService = defaultGroupCardsFactoryService;
        _tutorialService = tutorialService;
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

    public async Task ProgressTutorial(ulong userXuid, uint step) =>
        await _userDataRepository.UpdateTutorialStep(userXuid, step);

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

        // ReSharper disable once ArrangeMethodOrOperatorBody
        return await _userDataRepository.SetStartingCard(userInitializationData.Xuid,
            userInitializationData.FavoriteCharacterMasterCardId, masterTitleId);
    }

    private async Task InitializeDeck(UserData currentUserData, UserInitializationData userInitializationData)
    {
        BandCategory group = (BandCategory)(userInitializationData.FavoriteCharacterMasterId / 1000);

        List<Card> groupCards = await _defaultGroupCardsFactoryService.Create(group);

        await AddCardsAndCharactersToUser(userInitializationData.Xuid, groupCards,
            currentCards: currentUserData.CardList,
            currentCharacters: currentUserData.CharacterList);

        // Remove UR character duplicate
        List<ulong> cardIds = groupCards.Where(x => x.MasterCardId / 10000 != userInitializationData.FavoriteCharacterMasterId)
            .Select(x => x.Id)
            .ToList();

        // Set center card
        cardIds.Insert(4, userInitializationData.FavoriteCharacterCardId);

        // Take only 9 cards
        await _userDataRepository.SetDeck(userInitializationData.Xuid, 1, cardIds.Take(9));
    }

    public async Task AddCardsAndCharactersToUser(ulong xuid, List<Card> cards, List<Character>? characters = null,
        List<Card>? currentCards = null, List<Character>? currentCharacters = null)
    {
        if (currentCards is null || currentCharacters is null)
        {
            UserData? userData = await GetUserDataByXuid(xuid);
            Debug.Assert(userData is not null);

            currentCards = userData.CardList;
            currentCharacters = userData.CharacterList;
        }

        cards = cards.ExceptBy(currentCards, x => x.MasterCardId).ToList();

        characters ??= cards.Select(x => x.MasterCardId / 10000)
            .Select(x => new Character
            {
                MasterCharacterId = x,
                Exp = 0,
                BeforeExp = 0
            })
            .ToList();

        characters = characters.ExceptBy(currentCharacters, x => x.MasterCharacterId).ToList();

        await _userDataRepository.AddCardsAndCharacters(xuid, cards, characters);
    }

    public async Task<User?> UpdateUser(ulong xuid, string? name, string? comment, uint? favoriteMasterCardId, uint? guestSmileMasterCardId,
        uint? guestPureMasterCardId, uint? guestCoolMasterCardId, bool? friendRequestDisabled) =>
        await _userDataRepository.UpdateUser(xuid, name, comment, favoriteMasterCardId, guestSmileMasterCardId,
            guestPureMasterCardId, guestCoolMasterCardId, friendRequestDisabled);
}
