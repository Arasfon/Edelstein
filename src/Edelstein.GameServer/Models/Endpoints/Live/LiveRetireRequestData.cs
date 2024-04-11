using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveRetireRequestData(
    uint MasterLiveId,
    LiveLevel Level,
    LiveScore LiveScore
);
