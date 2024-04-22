using Edelstein.Data.Msts;

namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveGuestRequestData(
    uint MasterLiveId,
    LiveLevel Level
);
