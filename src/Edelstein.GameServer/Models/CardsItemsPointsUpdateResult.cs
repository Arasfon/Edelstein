using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models;

public record CardsItemsPointsUpdateResult(
    List<Card> Cards,
    List<Item> Items,
    List<Point> Points
);
