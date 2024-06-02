using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Transport;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Core;
using Edelstein.Server.Security;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class CoreController : Controller
{
    private readonly IUserService _userService;
    private readonly ITutorialService _tutorialService;

    public CoreController(IUserService userService, ITutorialService tutorialService)
    {
        _userService = userService;
        _tutorialService = tutorialService;
    }

    [Route("user")]
    public async Task<AsyncEncryptedResult> GetUser()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        UserData? userData = await _userService.GetUserDataByXuid(xuid);

        return AsyncEncryptedResult.Create(userData);
    }

    [HttpPost]
    [Route("user")]
    public async Task<AsyncEncryptedResult> UpdateUser(EncryptedRequest<UserUpdateRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        User user = await _userService.UpdateUser(xuid, encryptedRequest.DeserializedObject.Name,
            encryptedRequest.DeserializedObject.Comment, encryptedRequest.DeserializedObject.FavoriteMasterCardId,
            encryptedRequest.DeserializedObject.GuestSmileMasterCardId, encryptedRequest.DeserializedObject.GuestPureMasterCardId,
            encryptedRequest.DeserializedObject.GuestCoolMasterCardId, encryptedRequest.DeserializedObject.FriendRequestDisabled);

        return AsyncEncryptedResult.Create(new UserUpdateResponseData(user, []));
    }

    [HttpPost]
    [Route("user/initialize")]
    public async Task<AsyncEncryptedResult> InitializeUser(EncryptedRequest<UserInitializationRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        User updatedUser = await _userService.InitializeUserStartingCharacterAndDeck(xuid);

        return AsyncEncryptedResult.Create(updatedUser);
    }

    [HttpPost]
    [Route("tutorial")]
    public async Task<AsyncEncryptedResult> Tutorial(EncryptedRequest<TutorialRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _tutorialService.UpdateTutorialStep(xuid, encryptedRequest.DeserializedObject.Step);

        return AsyncEncryptedResult.Create();
    }

    [Route("home")]
    public async Task<AsyncEncryptedResult> Home()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        UserHomeDocument homeDocument = (await _userService.GetHomeByXuid(xuid).ConfigureAwait(false))!;

        IAsyncEnumerable<Gift> gifts = _userService.GetAllGifts(xuid);

        Home home = new()
        {
            GiftList = gifts,
            PendingFriendCount = 0,
            ClearMissionCount = 0,
            ClearBeginnerMissionCount = 0,
            BeginnerMissionComplete = false,
            NewAnnouncementFlag = homeDocument.HasUnreadAnnouncements,
            InformationList = [],
            PresetSetting = homeDocument.Presets,
            NotClearedDailyMissionCount = 0,
            UnreadStoryCount = 0,
            UnreadChatCount = homeDocument.ChatStorage.Chats.Count(x => !x.ChatProgress.IsRead),
            ActiveFriend = 0,
            SerialCodeIdList = []
        };

        return AsyncEncryptedResult.Create(new HomeResponseData(home, []));
    }

    [Route("mission")]
    public AsyncEncryptedResult Mission()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        return AsyncEncryptedResult.Create(new UserMissionsDocument { MissionList = [] });
    }

    [HttpPost]
    [Route("gift")]
    public async Task<AsyncEncryptedResult> ClaimGifts(EncryptedRequest<GiftRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        GiftClaimResult giftClaimResult = await _userService.ClaimGifts(xuid, encryptedRequest.DeserializedObject.GiftIds);

        return AsyncEncryptedResult.Create(new GiftResponseData(giftClaimResult.FailedGifts, giftClaimResult.UpdatedValueList,
            giftClaimResult.Rewards, []));
    }

    [Route("friend")]
    public AsyncEncryptedResult Friend() =>
        AsyncEncryptedResult.Create(new FriendResponseData([]));

    [HttpPost]
    [Route("login_bonus")]
    public AsyncEncryptedResult LoginBonus() =>
        AsyncEncryptedResult.Create(new LoginBonusResponseData([], 0, []));

    [HttpPost]
    [Route("deck")]
    public async Task<AsyncEncryptedResult> SetDeck(EncryptedRequest<DeckRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Deck deck = await _userService.UpdateDeck(xuid, encryptedRequest.DeserializedObject.Slot,
            encryptedRequest.DeserializedObject.MainCardIds);

        return AsyncEncryptedResult.Create(new DeckResponseData(deck, []));
    }
}
