using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveEndRequestData(
    uint MasterLiveId,
    Edelstein.Data.Msts.LiveLevel Level,
    LiveScore LiveScore,
    uint UseLp,
    uint UseIcon
);
