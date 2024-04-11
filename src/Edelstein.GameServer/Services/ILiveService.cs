using Edelstein.GameServer.Models;
using Edelstein.GameServer.Models.Endpoints.Live;

namespace Edelstein.GameServer.Services;

public interface ILiveService
{
    public Task StartLive(ulong xuid, LiveStartRequestData liveStartData);
    public Task RetireLive(ulong xuid, LiveRetireRequestData liveRetireData);
    public Task<LiveFinishResult> FinishLive(ulong xuid, LiveEndRequestData liveFinishData);
}
