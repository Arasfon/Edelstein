using Edelstein.Data.Msts;

namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveContinueRequestData(uint MasterLiveId, LiveLevel Level);
