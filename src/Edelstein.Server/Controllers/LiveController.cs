using Edelstein.Data.Constants;
using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Transport;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Live;
using Edelstein.Server.Security;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api/live")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class LiveController : Controller
{
    private readonly ILiveService _liveService;

    public LiveController(ILiveService liveService) =>
        _liveService = liveService;

    [Route("clearRate")]
    public EncryptedResult ClearRate() =>
        new EncryptedResponse<LiveClearRateResponseData>(new LiveClearRateResponseData(null!, MasterMusicIds.Get(), []));

    [Route("guest")]
    public EncryptedResult Guest(EncryptedRequest<LiveGuestRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        //if (await _tutorialService.IsTutorialInProgress(xuid))
        return new EncryptedResponse<LiveGuestResponseData>(new LiveGuestResponseData([Friend.GetTutorial()]));
    }

    [Route("mission")]
    public EncryptedResult Mission(EncryptedRequest<LiveMissionRequestData> encryptedRequest)
    {
        // TODO: Live mission ranking
        //ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        //await _liveService.GetUserMissionsRanking(xuid, encryptedRequest.DeserializedObject);

        // ReSharper disable once ArrangeMethodOrOperatorBody
        return new EncryptedResponse<LiveMissionResponseData>(new LiveMissionResponseData("00.00%", "00.00%", "00.00%"));
    }

    [Route("reward")]
    public async Task<EncryptedResult> Rewards(EncryptedRequest<LiveRewardsRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        LiveRewardsRetrievalResult liveRewardsRetrievalResult =
            await _liveService.GetLiveRewards(xuid, encryptedRequest.DeserializedObject.MasterLiveId);

        return new EncryptedResponse<LiveRewardsResponseData>(new LiveRewardsResponseData(liveRewardsRetrievalResult.EnsuredRewards,
            liveRewardsRetrievalResult.RandomRewards));
    }

    [Route("ranking")]
    public EncryptedResult Ranking(EncryptedRequest<LiveRankingRequestData> encryptedRequest) =>
        new EncryptedResponse<LiveRankingResponseData>(new LiveRankingResponseData([]));

    [Route("start")]
    public async Task<EncryptedResult> Start(EncryptedRequest<LiveStartRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _liveService.StartLive(xuid, encryptedRequest.DeserializedObject);

        return EmptyEncryptedResponseFactory.Create();
    }

    [Route("skip")]
    public async Task<EncryptedResult> Skip(EncryptedRequest<LiveSkipRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        // TODO: Fix separation of concerns (replace passing request data with parameters)
        LiveFinishResult liveFinishResult = await _liveService.SkipLive(xuid, encryptedRequest.DeserializedObject);

        if (liveFinishResult.Status is not LiveFinishResultStatus.Success)
            return EmptyEncryptedResponseFactory.Create(ErrorCode.ErrorItemShortage);

        return new EncryptedResponse<LiveSkipResponseData>(new LiveSkipResponseData
        {
            ItemList = liveFinishResult.ChangedItems,
            PointList = liveFinishResult.ChangedPoints,
            Live = liveFinishResult.FinishedLiveData,
            ClearMasterLiveMissionIds = liveFinishResult.ClearedMasterLiveMissionIds,
            User = liveFinishResult.UpdatedUserData.User,
            Stamina = liveFinishResult.UpdatedUserData.Stamina,
            CharacterList = liveFinishResult.UpdatedCharacters,
            RewardList = liveFinishResult.Rewards,
            GiftList = liveFinishResult.Gifts,
            ClearMissionIds = liveFinishResult.ClearedMissionIds,
            EventPointRewardList = liveFinishResult.EventPointRewards,
            RankingChange = liveFinishResult.RankingChange,
            EventRankingData = liveFinishResult.EventRankingData
        });
    }

    [Route("retire")]
    public async Task<EncryptedResult> Retire(EncryptedRequest<LiveRetireRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _liveService.RetireLive(xuid, encryptedRequest.DeserializedObject);

        return new EncryptedResponse<LiveRetireResponseData>(new LiveRetireResponseData(new Stamina(), [], []));
    }

    [Route("continue")]
    public async Task<EncryptedResult> Continue(EncryptedRequest<LiveContinueRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Gem? gem = await _liveService.ContinueLive(xuid, encryptedRequest.DeserializedObject.MasterLiveId, encryptedRequest.DeserializedObject.Level);

        if (gem is null)
            throw new Exception("Not enough gems available");

        return new EncryptedResponse<LiveContinueResponseData>(new LiveContinueResponseData(gem));
    }

    [Route("end")]
    public async Task<EncryptedResult> End(EncryptedRequest<LiveEndRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        // TODO: Fix separation of concerns (replace passing request data with parameters)
        LiveFinishResult liveFinishResult = await _liveService.FinishLive(xuid, encryptedRequest.DeserializedObject);

        if (liveFinishResult.Status is not LiveFinishResultStatus.Success)
            return EmptyEncryptedResponseFactory.Create(ErrorCode.ErrorItemShortage);

        return new EncryptedResponse<LiveEndResponseData>(new LiveEndResponseData
        {
            Gem = liveFinishResult.ChangedGem,
            ItemList = liveFinishResult.ChangedItems,
            PointList = liveFinishResult.ChangedPoints,
            Live = liveFinishResult.FinishedLiveData,
            ClearMasterLiveMissionIds = liveFinishResult.ClearedMasterLiveMissionIds,
            User = liveFinishResult.UpdatedUserData.User,
            Stamina = liveFinishResult.UpdatedUserData.Stamina,
            CharacterList = liveFinishResult.UpdatedCharacters,
            RewardList = liveFinishResult.Rewards,
            GiftList = liveFinishResult.Gifts,
            ClearMissionIds = liveFinishResult.ClearedMissionIds,
            EventPointRewardList = liveFinishResult.EventPointRewards,
            RankingChange = liveFinishResult.RankingChange,
            EventRankingData = liveFinishResult.EventRankingData
        });
    }
}
