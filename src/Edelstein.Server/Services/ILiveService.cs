using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Live;

namespace Edelstein.Server.Services;

public interface ILiveService
{
    public Task StartLive(ulong xuid, LiveStartRequestData liveStartData);
    public Task RetireLive(ulong xuid, LiveRetireRequestData liveRetireData);
    public Task<LiveFinishResult> FinishLive(ulong xuid, LiveEndRequestData liveFinishData);
}
