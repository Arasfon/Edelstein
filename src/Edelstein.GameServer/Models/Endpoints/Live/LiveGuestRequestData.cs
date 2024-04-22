using Edelstein.Data.Msts;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveGuestRequestData(
    uint MasterLiveId,
    LiveLevel Level
);
