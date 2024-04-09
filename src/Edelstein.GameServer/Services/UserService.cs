using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.GameServer.Repositories;

namespace Edelstein.GameServer.Services;

public class UserService : IUserService
{
    private readonly IAuthenticationDataRepository _authenticationDataRepository;
    private readonly IUserDataRepository _userDataRepository;
    private readonly IUserHomeRepository _userHomeRepository;
    private readonly IUserMissionsRepository _userMissionsRepository;
    private readonly IUserInitializationDataRepository _userInitializationDataRepository;
    private readonly IDefaultGroupCardsFactoryService _defaultGroupCardsFactoryService;

    public UserService(
        IAuthenticationDataRepository authenticationDataRepository,
        IUserDataRepository userDataRepository,
        IUserHomeRepository userHomeRepository,
        IUserMissionsRepository userMissionsRepository,
        IUserInitializationDataRepository userInitializationDataRepository,
        IDefaultGroupCardsFactoryService defaultGroupCardsFactoryService)
    {
        _authenticationDataRepository = authenticationDataRepository;
        _userDataRepository = userDataRepository;
        _userHomeRepository = userHomeRepository;
        _userMissionsRepository = userMissionsRepository;
        _userInitializationDataRepository = userInitializationDataRepository;
        _defaultGroupCardsFactoryService = defaultGroupCardsFactoryService;
    }

    public async Task<AuthenticationData?> GetAuthenticationDataByXuid(ulong xuid) =>
        await _authenticationDataRepository.GetByXuid(xuid);

    public async Task<UserData?> GetUserDataByXuid(ulong xuid) =>
        await _userDataRepository.GetByXuid(xuid);

    public async Task<UserHomeDocument?> GetHomeByXuid(ulong xuid) =>
        await _userHomeRepository.GetByXuid(xuid);

    public async Task<UserMissionsDocument?> GetUserMissionsByXuid(ulong xuid) =>
        await _userMissionsRepository.GetUserMissionsByXuid(xuid);

    public async Task ProgressTutorial(ulong userXuid, uint step) =>
        await _userDataRepository.UpdateTutorialStep(userXuid, step);

    public async Task<User> InitializeUserStartingCharacterAndDeck(ulong xuid)
    {
        UserInitializationData userInitializationData = await _userInitializationDataRepository.GetByXuid(xuid);

        User updatedUser = await InitializeStartingCardAndTitle(userInitializationData);
        await _userHomeRepository.InitializePresets(xuid, userInitializationData.FavoriteCharacterMasterCardId);
        await InitializeDeck(userInitializationData);
        await AddCharacter(xuid, userInitializationData.FavoriteCharacterMasterId);

        return updatedUser;
    }

    private async Task<User> InitializeStartingCardAndTitle(UserInitializationData userInitializationData)
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
        return (await _userDataRepository.SetStartingCard(userInitializationData.Xuid,
            userInitializationData.FavoriteCharacterMasterCardId, masterTitleId)).User;
    }

    private async Task InitializeDeck(UserInitializationData userInitializationData)
    {
        BandCategory group = (BandCategory)(userInitializationData.FavoriteCharacterMasterId / 1000);

        List<Card> groupCards = await _defaultGroupCardsFactoryService.Create(group);

        await AddCardsToUser(userInitializationData.Xuid, groupCards);

        // Remove UR character duplicate
        List<ulong> cardIds = groupCards.Select(x => x.Id)
            .Where(x => x / 10000 != userInitializationData.FavoriteCharacterMasterId)
            .ToList();

        // Set center card
        cardIds.RemoveAt((int)userInitializationData.FavoriteCharacterMasterId % 10 - 1);
        cardIds.Insert(4, userInitializationData.FavoriteCharacterCardId);

        // Take only 9 cards
        await _userDataRepository.SetDeck(userInitializationData.Xuid, 1, cardIds.Take(9));
    }

    public async Task AddCardsToUser(ulong xuid, IEnumerable<Card> cards) =>
        await _userDataRepository.AddCards(xuid, cards);

    public async Task AddCharacter(ulong xuid, uint masterCharacterId) =>
        await _userDataRepository.AddCharacter(xuid, masterCharacterId);
}
