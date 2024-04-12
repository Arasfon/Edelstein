using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Transport;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models.Endpoints.Core;
using Edelstein.GameServer.Security;
using Edelstein.GameServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

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

        User? user = await _userService.UpdateUser(xuid, encryptedRequest.DeserializedObject.Name,
            encryptedRequest.DeserializedObject.Comment, encryptedRequest.DeserializedObject.FavoriteMasterCardId,
            encryptedRequest.DeserializedObject.GuestSmileMasterCardId, encryptedRequest.DeserializedObject.GuestPureMasterCardId,
            encryptedRequest.DeserializedObject.GuestCoolMasterCardId, encryptedRequest.DeserializedObject.FriendRequestDisabled);

        return new EncryptedResponse<User?>(user);
    }

    [HttpPost]
    [Route("user/initialize")]
    public async Task<EncryptedResult> InitializeUser(EncryptedRequest<UserInitializationRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        User updatedUser = await _userService.InitializeUserStartingCharacterAndDeck(xuid);
        await _tutorialService.FinishLotteryTutorial(xuid);

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

        UserHomeDocument? home = await _userService.GetHomeByXuid(xuid);

        return new EncryptedResponse<UserHomeDocument?>(home);
    }

    [Route("mission")]
    public async Task<EncryptedResult> Mission()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        UserMissionsDocument? missions = await _userService.GetUserMissionsByXuid(xuid);

        return new EncryptedResponse<UserMissionsDocument?>(missions);
    }

    [Route("friend")]
    public EncryptedResult Friend() =>
        new EncryptedResponse<FriendResponseData>(new FriendResponseData([]));

    [HttpPost]
    [Route("login_bonus")]
    public EncryptedResult LoginBonus() =>
        new EncryptedResponse<LoginBonusResponseData>(new LoginBonusResponseData([], 0, []));
}
