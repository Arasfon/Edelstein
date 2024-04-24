using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Live;

namespace Edelstein.Server.Services;

public interface ILiveService
{
    public Task StartLive(ulong xuid, LiveStartRequestData liveStartData);
    public Task RetireLive(ulong xuid, LiveRetireRequestData liveRetireData);
    public Task<LiveFinishResult> SkipLive(ulong xuid, LiveSkipRequestData liveSkipData);
    public Task<LiveFinishResult> FinishLive(ulong xuid, LiveEndRequestData liveFinishData);
    public Task<LiveRewardsRetrievalResult> GetLiveRewards(ulong xuid, uint masterLiveId);
    public Task<Gem?> ContinueLive(ulong xuid, uint masterLiveId, LiveLevel liveLevel);
}
