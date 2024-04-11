using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveStartRequestData(
    uint MasterLiveId,
    LiveLevel Level,
    uint DeckSlot,
    byte LiveBoost,
    byte AutoPlay,
    uint MasterEventId,
    byte IsOmakase
);
