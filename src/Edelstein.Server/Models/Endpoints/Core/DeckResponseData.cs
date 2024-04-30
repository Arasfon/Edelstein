using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Core;

public record DeckResponseData(
    Deck Deck,
    List<uint> ClearMissionIds
);
