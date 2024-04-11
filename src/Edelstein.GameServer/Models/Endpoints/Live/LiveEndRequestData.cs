using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveEndRequestData(
    uint MasterLiveId,
    LiveLevel Level,
    LiveScore LiveScore,
    uint UseLp,
    uint UseIcon
);