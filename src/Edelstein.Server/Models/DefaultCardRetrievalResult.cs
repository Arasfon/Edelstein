using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models;

public record DefaultCardRetrievalResult(
    int DuplicateCount,
    List<Card> Cards
);
