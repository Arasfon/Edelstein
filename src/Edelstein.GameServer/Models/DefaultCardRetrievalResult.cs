using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models;

public record DefaultCardRetrievalResult(int DuplicateCount, List<Card> Cards);
