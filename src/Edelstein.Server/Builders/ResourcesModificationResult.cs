using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class ResourcesModificationResult
{
    public required UpdatedValueList Updates { get; set; }

    public required List<Reward>? Rewards { get; set; }
    public required List<Gift>? Gifts { get; set; }

    public required Gem? Gem { get; set; }
    public required List<Point> Points { get; set; }
    public required List<Card> Cards { get; set; }
    public required List<Item> Items { get; set; }
}
