using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveGuestRequestData(
    uint MasterLiveId,
    LiveLevel Level
);
