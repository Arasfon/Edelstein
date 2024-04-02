namespace Edelstein.Models.Protocol;

public class Item
{
    public ulong Id { get; set; }
    public uint MasterItemId { get; set; }
    public int Amount { get; set; }
    public long? ExpireDateTime { get; set; }
}
