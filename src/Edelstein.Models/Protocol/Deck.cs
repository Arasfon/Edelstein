namespace Edelstein.Models.Protocol;

public class Deck
{
    public byte Slot { get; set; }
    public List<ulong> MainCardIds { get; set; } = [];
}
