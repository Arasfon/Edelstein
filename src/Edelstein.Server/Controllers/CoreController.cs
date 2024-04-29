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
    private readonly IUserGiftsService _userGiftsService;

    public CoreController(IUserService userService, ITutorialService tutorialService, IUserGiftsService userGiftsService)
    {
        _userService = userService;
        _tutorialService = tutorialService;
        _userGiftsService = userGiftsService;
    }

    [Route("user")]
    public async Task<EncryptedResult> GetUser()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        UserData? userData = await _userService.GetUserDataByXuid(xuid);

        return new EncryptedResponse<UserData?>(userData);
    }

    [HttpPost]
    [Route("user")]
    public async Task<EncryptedResult> UpdateUser(EncryptedRequest<UserUpdateRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        User user = await _userService.UpdateUser(xuid, encryptedRequest.DeserializedObject.Name,
            encryptedRequest.DeserializedObject.Comment, encryptedRequest.DeserializedObject.FavoriteMasterCardId,
            encryptedRequest.DeserializedObject.GuestSmileMasterCardId, encryptedRequest.DeserializedObject.GuestPureMasterCardId,
            encryptedRequest.DeserializedObject.GuestCoolMasterCardId, encryptedRequest.DeserializedObject.FriendRequestDisabled);

        return new EncryptedResponse<UserUpdateResponseData>(new UserUpdateResponseData(user, []));
    }

    [HttpPost]
    [Route("user/initialize")]
    public async Task<EncryptedResult> InitializeUser(EncryptedRequest<UserInitializationRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        User updatedUser = await _userService.InitializeUserStartingCharacterAndDeck(xuid);

        return new EncryptedResponse<User>(updatedUser);
    }

    [HttpPost]
    [Route("tutorial")]
    public async Task<EncryptedResult> Tutorial(EncryptedRequest<TutorialRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _tutorialService.UpdateTutorialStep(xuid, encryptedRequest.DeserializedObject.Step);

        return EmptyEncryptedResponseFactory.Create();
    }

    [Route("home")]
    public async Task<EncryptedResult> Home()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        UserHomeDocument homeDocument = (await _userService.GetHomeByXuid(xuid))!;

        IEnumerable<Gift> gifts = await _userGiftsService.GetAllGifts(xuid);

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

        return new EncryptedResponse<HomeResponseData>(new HomeResponseData(home, []));
    }

    [Route("mission")]
    public EncryptedResult Mission()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        return new EncryptedResponse<UserMissionsDocument?>(new UserMissionsDocument { MissionList = [] });
    }

    [HttpPost]
    [Route("gift")]
    public async Task<EncryptedResult> ClaimGifts(EncryptedRequest<GiftRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        GiftClaimResult giftClaimResult = await _userGiftsService.ClaimGifts(xuid, encryptedRequest.DeserializedObject.GiftIds);

        return new EncryptedResponse<GiftResponseData>(new GiftResponseData(giftClaimResult.FailedGifts, giftClaimResult.UpdatedValueList,
            giftClaimResult.Rewards, []));
    }

    [Route("friend")]
    public EncryptedResult Friend() =>
        new EncryptedResponse<FriendResponseData>(new FriendResponseData([]));

    [HttpPost]
    [Route("login_bonus")]
    public EncryptedResult LoginBonus() =>
        new EncryptedResponse<LoginBonusResponseData>(new LoginBonusResponseData([], 0, []));

    [HttpPost]
    [Route("deck")]
    public async Task<EncryptedResult> SetDeck(EncryptedRequest<DeckRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Deck deck = await _userService.UpdateDeck(xuid, encryptedRequest.DeserializedObject.Slot, encryptedRequest.DeserializedObject.MainCardIds);

        return new EncryptedResponse<DeckResponseData>(new DeckResponseData(deck, []));
    }
}
