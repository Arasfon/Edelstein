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
    private readonly ITutorialService _tutorialService;

    public LotteryController(IUserService userService, ILotteryService lotteryService, ITutorialService tutorialService)
    {
        _userService = userService;
        _lotteryService = lotteryService;
        _tutorialService = tutorialService;
    }

    [HttpPost]
    [Route("get_tutorial")]
    public async Task<EncryptedResult> GetTutorial(EncryptedRequest<LotteryTutorialRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Lottery tutorialLottery =
            await _lotteryService.GetTutorialLotteryByMasterCharacterId(encryptedRequest.DeserializedObject.MasterCharacterId);

        await _tutorialService.StartLotteryTutorial(xuid, encryptedRequest.DeserializedObject.MasterCharacterId);

        return new EncryptedResponse<LotteryTutorialResponseData>(new LotteryTutorialResponseData([tutorialLottery], []));
    }

    [HttpPost]
    [Route("")]
    public async Task<EncryptedResult> DrawLottery(EncryptedRequest<Lottery> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        OneOf<LotteryDrawResult, TutorialLotteryDrawResult> lotteryDrawResultUnion =
            await _lotteryService.Draw(encryptedRequest.DeserializedObject);

        LotteryDrawResult lotteryDrawResult = await lotteryDrawResultUnion.Match<Task<LotteryDrawResult>>(Task.FromResult,
            async tutorialLotteryDrawResult =>
            {
                await _tutorialService.ProgressLotteryTutorialWithDrawnCard(xuid, tutorialLotteryDrawResult.FavoriteCardMasterId,
                    tutorialLotteryDrawResult.FavoriteCardId);

                return tutorialLotteryDrawResult;
            });

        await _userService.AddCardsAndCharactersToUser(xuid, lotteryDrawResult.Cards);

        return new EncryptedResponse<DrawLotteryResponseData>(new DrawLotteryResponseData(lotteryDrawResult.LotteryItems,
            new UpdatedValueList { CardList = lotteryDrawResult.Cards }, [], [], []));
    }
}
