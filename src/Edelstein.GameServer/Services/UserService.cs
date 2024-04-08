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
        await InitializeDeck(userInitializationData);
        await AddCharacter(xuid, userInitializationData.FavoriteCharacterMasterId);

        return updatedUser;
    }

    public async Task InitializeLotteryTutorial(ulong xuid, uint favoriteCharacterMasterId) =>
        await _userInitializationDataRepository.CreateForCharacter(xuid, favoriteCharacterMasterId);

    public async Task UpdateLotteryTutorialWithDrawnCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId) =>
        await _userInitializationDataRepository.UpdateWithDrawedCard(xuid, favoriteCharacterMasterCardId, favoriteCharacterCardId);

    public async Task CompleteLotteryTutorial(ulong xuid) =>
        await _userInitializationDataRepository.Delete(xuid);

    private async Task<User> InitializeStartingCardAndTitle(UserInitializationData userInitializationData)
    {
        // HACK: Dummy data for Ren Hazuki

        // TODO: ulong masterTitleId = _titleMstProvider.GetForCharacterMasterId(tutorialData.FavoriteCharacterMasterId);

        // ReSharper disable once ArrangeMethodOrOperatorBody
        return (await _userDataRepository.SetStartingCard(userInitializationData.Xuid,
            userInitializationData.FavoriteCharacterMasterCardId, 3000035)).User;
    }

    private async Task InitializeDeck(UserInitializationData userInitializationData)
    {
        // HACK: Dummy data for Ren Hazuki

        // TODO: Query group by masterCharacterId
        BandCategory bandCategory = BandCategory.Liella;

        List<Card> groupCards = await _defaultGroupCardsFactoryService.Create(bandCategory);

        await AddCardsToUser(userInitializationData.Xuid, groupCards);

        // TODO: Remove non-UR character card duplicate and move everything out of center (shift deck, not replace)
        List<ulong> cardIds = groupCards.Select(x => x.Id).ToList();

        // Set center card
        cardIds[4] = userInitializationData.FavoriteCharacterCardId;

        await _userDataRepository.SetDeck(userInitializationData.Xuid, 1, cardIds);
    }

    public async Task AddCardsToUser(ulong xuid, IEnumerable<Card> cards) =>
        await _userDataRepository.AddCards(xuid, cards);

    public async Task AddCharacter(ulong xuid, uint masterCharacterId) =>
        await _userDataRepository.AddCharacter(xuid, masterCharacterId);
}
