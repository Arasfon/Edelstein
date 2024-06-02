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
    public AsyncEncryptedResult ClearRate() =>
        AsyncEncryptedResult.Create(new LiveClearRateResponseData(null!, MasterMusicIds.Get(), []));

    [Route("guest")]
    public AsyncEncryptedResult Guest(EncryptedRequest<LiveGuestRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        //if (await _tutorialService.IsTutorialInProgress(xuid))
        return AsyncEncryptedResult.Create(new LiveGuestResponseData([Friend.GetTutorial()]));
    }

    [Route("mission")]
    public AsyncEncryptedResult Mission(EncryptedRequest<LiveMissionRequestData> encryptedRequest)
    {
        // TODO: Live mission ranking
        //ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        //await _liveService.GetUserMissionsRanking(xuid, encryptedRequest.DeserializedObject);

        // ReSharper disable once ArrangeMethodOrOperatorBody
        return AsyncEncryptedResult.Create(new LiveMissionResponseData("00.00%", "00.00%", "00.00%"));
    }

    [Route("reward")]
    public async Task<AsyncEncryptedResult> Rewards(EncryptedRequest<LiveRewardsRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        LiveRewardsRetrievalResult liveRewardsRetrievalResult =
            await _liveService.GetLiveRewards(xuid, encryptedRequest.DeserializedObject.MasterLiveId);

        return AsyncEncryptedResult.Create(new LiveRewardsResponseData(liveRewardsRetrievalResult.EnsuredRewards,
            liveRewardsRetrievalResult.RandomRewards));
    }

    [Route("ranking")]
    public AsyncEncryptedResult Ranking(EncryptedRequest<LiveRankingRequestData> encryptedRequest) =>
        AsyncEncryptedResult.Create(new LiveRankingResponseData([]));

    [Route("start")]
    public async Task<AsyncEncryptedResult> Start(EncryptedRequest<LiveStartRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _liveService.StartLive(xuid, encryptedRequest.DeserializedObject);

        return AsyncEncryptedResult.Create();
    }

    [Route("skip")]
    public async Task<AsyncEncryptedResult> Skip(EncryptedRequest<LiveSkipRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        // TODO: Fix separation of concerns (replace passing request data with parameters)
        LiveFinishResult liveFinishResult = await _liveService.SkipLive(xuid, encryptedRequest.DeserializedObject);

        if (liveFinishResult.Status is not LiveFinishResultStatus.Success)
            return AsyncEncryptedResult.Create(ErrorCode.ErrorItemShortage);

        return AsyncEncryptedResult.Create(new LiveSkipResponseData
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
    public async Task<AsyncEncryptedResult> Retire(EncryptedRequest<LiveRetireRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _liveService.RetireLive(xuid, encryptedRequest.DeserializedObject);

        return AsyncEncryptedResult.Create(new LiveRetireResponseData(new Stamina(), [], []));
    }

    [Route("continue")]
    public async Task<AsyncEncryptedResult> Continue(EncryptedRequest<LiveContinueRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        Gem? gem = await _liveService.ContinueLive(xuid, encryptedRequest.DeserializedObject.MasterLiveId,
            encryptedRequest.DeserializedObject.Level);

        if (gem is null)
            throw new Exception("Not enough gems available");

        return AsyncEncryptedResult.Create(new LiveContinueResponseData(gem));
    }

    [Route("end")]
    public async Task<AsyncEncryptedResult> End(EncryptedRequest<LiveEndRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        // TODO: Fix separation of concerns (replace passing request data with parameters)
        LiveFinishResult liveFinishResult = await _liveService.FinishLive(xuid, encryptedRequest.DeserializedObject);

        if (liveFinishResult.Status is not LiveFinishResultStatus.Success)
            return AsyncEncryptedResult.Create(ErrorCode.ErrorItemShortage);

        return AsyncEncryptedResult.Create(new LiveEndResponseData
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
