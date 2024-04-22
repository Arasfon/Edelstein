using Edelstein.Data.Models;
using Edelstein.Data.Msts;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveStartRequestData(
    uint MasterLiveId,
    LiveLevel Level,
    uint DeckSlot,
    byte LiveBoost,
    byte AutoPlay,
    uint MasterEventId,
    byte IsOmakase
)
{
    public CurrentLiveData ToCurrentLiveData() =>
        new()
        {
            MasterLiveId = MasterLiveId,
            Level = Level,
            DeckSlot = DeckSlot,
            LiveBoost = LiveBoost,
            AutoPlay = AutoPlay,
            MasterEventId = MasterEventId,
            IsOmakase = IsOmakase
        };
}
