using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveRetireRequestData(
    uint MasterLiveId,
    LiveLevel Level,
    LiveScore LiveScore
);
