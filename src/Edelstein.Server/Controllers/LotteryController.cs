using Edelstein.Data.Extensions;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Transport;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Lottery;
using Edelstein.Server.Security;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api/lottery")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class LotteryController : Controller
{
    private readonly IUserService _userService;
    private readonly ILotteryService _lotteryService;
    private readonly ITutorialService _tutorialService;

    public LotteryController(IUserService userService, ILotteryService lotteryService, ITutorialService tutorialService)
    {
        _userService = userService;
        _lotteryService = lotteryService;
        _tutorialService = tutorialService;
    }

    [HttpPost]
    [Route("get_tutorial")]
    public async Task<AsyncEncryptedResult> GetTutorial(EncryptedRequest<LotteryTutorialRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Lottery tutorialLottery =
            await _lotteryService.GetTutorialLotteryByMasterCharacterId(encryptedRequest.DeserializedObject.MasterCharacterId);

        await _tutorialService.InitializeUserTutorialData(xuid, encryptedRequest.DeserializedObject.MasterCharacterId);

        return AsyncEncryptedResult.Create(new LotteryTutorialResponseData([tutorialLottery], []));
    }

    [Route("")]
    public async Task<AsyncEncryptedResult> GetLotteries()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        List<Lottery> lotteries = await _lotteryService.GetAndRefreshUserLotteriesData(xuid);

        return AsyncEncryptedResult.Create(new GetLotteriesResponseData(lotteries, []));
    }

    [HttpPost]
    [Route("")]
    public async Task<AsyncEncryptedResult> DrawLottery(EncryptedRequest<Lottery> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        LotteryDrawResult lotteryDrawResult = await _lotteryService.Draw(xuid, encryptedRequest.DeserializedObject);

        if (lotteryDrawResult.Status != LotteryDrawResultStatus.Success)
            return AsyncEncryptedResult.Create(ErrorCode.ErrorItemShortage);

        if (await _lotteryService.IsTutorial(encryptedRequest.DeserializedObject))
        {
            Card urCard = lotteryDrawResult.Updates.CardList[^1];

            await _tutorialService.ProgressTutorialWithDrawnCard(xuid, urCard.MasterCardId, urCard.Id);

            await _userService.AddCharacter(xuid, urCard.MasterCardId / 10000, 1);
        }

        return AsyncEncryptedResult.Create(new DrawLotteryResponseData(lotteryDrawResult.LotteryItems,
            lotteryDrawResult.Updates, [], [], []));
    }
}
