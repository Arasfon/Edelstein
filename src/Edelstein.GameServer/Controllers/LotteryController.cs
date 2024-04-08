using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Transport;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models.Endpoints.Lottery;
using Edelstein.GameServer.Security;
using Edelstein.GameServer.Services;

using Microsoft.AspNetCore.Mvc;

using OneOf;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/lottery")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class LotteryController : Controller
{
    private readonly IUserService _userService;
    private readonly ILotteryService _lotteryService;

    public LotteryController(IUserService userService, ILotteryService lotteryService)
    {
        _userService = userService;
        _lotteryService = lotteryService;
    }

    [HttpPost]
    [Route("get_tutorial")]
    public async Task<EncryptedResult> GetTutorial(EncryptedRequest<LotteryTutorialRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Lottery tutorialLottery =
            await _lotteryService.GetTutorialLotteryByMasterCharacterId(encryptedRequest.DeserializedObject.MasterCharacterId);

        await _userService.InitializeLotteryTutorial(xuid, encryptedRequest.DeserializedObject.MasterCharacterId);

        return new EncryptedResponse<LotteryTutorialResponseData>(new LotteryTutorialResponseData([tutorialLottery], []));
    }

    [HttpPost]
    [Route("")]
    public async Task<EncryptedResult> DrawLottery(EncryptedRequest<Lottery> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        OneOf<LotteryDrawResult, TutorialLotteryDrawResult> lotteryDrawResultUnion =
            await _lotteryService.Draw(encryptedRequest.DeserializedObject);

        LotteryDrawResult lotteryDrawResult = await lotteryDrawResultUnion.Match<Task<LotteryDrawResult>>(async lotteryDrawResult =>
            {
                await _userService.AddCardsToUser(xuid, lotteryDrawResult.Cards);

                return lotteryDrawResult;
            },
            async tutorialLotteryDrawResult =>
            {
                await _userService.AddCardsToUser(xuid, tutorialLotteryDrawResult.Cards);

                await _userService.UpdateLotteryTutorialWithDrawnCard(xuid, tutorialLotteryDrawResult.FavoriteCardMasterId,
                    tutorialLotteryDrawResult.FavoriteCardId);

                return tutorialLotteryDrawResult;
            });

        return new EncryptedResponse<DrawLotteryResponseData>(new DrawLotteryResponseData(lotteryDrawResult.LotteryItems,
            new UpdatedValueList { CardList = lotteryDrawResult.Cards }, [], [], []));
    }
}
