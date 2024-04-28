using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class ResourcesModificationResult
{
    public required UpdatedValueList Updates { get; set; }

    public required LinkedList<Reward>? Rewards { get; set; }
    public required LinkedList<Gift>? Gifts { get; set; }

    public required Gem? Gem { get; set; }
    public required LinkedList<Point> Points { get; set; }
    public required LinkedList<Card> Cards { get; set; }
    public required LinkedList<Item> Items { get; set; }
}
