namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveSkipRequestData(
    uint MasterLiveId,
    uint DeckSlot,
    byte LiveBoost,
    uint MasterEventId
);
