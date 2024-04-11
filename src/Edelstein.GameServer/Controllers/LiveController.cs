using Edelstein.Data.Constants;
using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Transport;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models;
using Edelstein.GameServer.Models.Endpoints.Live;
using Edelstein.GameServer.Security;
using Edelstein.GameServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/live")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class LiveController : Controller
{
    private readonly IUserService _userService;
    private readonly ILiveService _liveService;
    private readonly ILiveClearRateProvider _liveClearRateProvider;
    private readonly ITutorialService _tutorialService;

    public LiveController(IUserService userService, ILiveService liveService, ILiveClearRateProvider liveClearRateProvider,
        ITutorialService tutorialService)
    {
        _userService = userService;
        _liveService = liveService;
        _liveClearRateProvider = liveClearRateProvider;
        _tutorialService = tutorialService;
    }

    [Route("clearRate")]
    public async Task<EncryptedResult> ClearRate()
    {
        List<AllUserClearRate> clearRates = await _liveClearRateProvider.GetAll();

        return new EncryptedResponse<LiveClearRateResponseData>(
            new LiveClearRateResponseData(clearRates, MasterMusicIds.Get().ToList(), []));
    }

    [Route("guest")]
    public EncryptedResult Guest(EncryptedRequest<LiveGuestRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        //if (await _tutorialService.IsTutorialInProgress(xuid))

        return new EncryptedResponse<LiveGuestResponseData>(new LiveGuestResponseData([Friend.GetTutorial()]));

        //throw new NotImplementedException();
    }

    [Route("start")]
    public EncryptedResult Start(EncryptedRequest<LiveStartRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        _liveService.StartLive(xuid, encryptedRequest.DeserializedObject);

        return EmptyEncryptedResponseFactory.Create();
    }

    [Route("retire")]
    public EncryptedResult Retire(EncryptedRequest<LiveRetireRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        _liveService.RetireLive(xuid, encryptedRequest.DeserializedObject);

        return new EncryptedResponse<LiveRetireResponseData>(new LiveRetireResponseData(new Stamina(), [], []));
    }

    [Route("end")]
    public async Task<EncryptedResult> End(EncryptedRequest<LiveEndRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        LiveFinishResult liveFinishResult = await _liveService.FinishLive(xuid, encryptedRequest.DeserializedObject);

        return new EncryptedResponse<LiveEndResponseData>(new LiveEndResponseData
        {
            Gem = liveFinishResult.ChangedGem,
            ItemList = liveFinishResult.ChangedItems,
            PointList = liveFinishResult.ChangedPoints,
            Live = liveFinishResult.PreviousLiveData,
            ClearMasterLiveMissionIds = liveFinishResult.ClearedMasterLiveMissionIds,
            User = liveFinishResult.UpdatedUserData.User,
            Stamina = liveFinishResult.UpdatedUserData.Stamina,
            CharacterList = liveFinishResult.DeckCharacters,
            RewardList = liveFinishResult.Rewards,
            GiftList = liveFinishResult.NewGifts,
            ClearMissionIds = liveFinishResult.ClearedMissionIds,
            EventPointRewardList = liveFinishResult.EventPointRewards,
            RankingChange = liveFinishResult.RankingChange,
            EventRankingData = liveFinishResult.EventRankingData
        });
    }
}
