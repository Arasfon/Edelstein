namespace Edelstein.Models.Protocol;

public class EventPoint
{
    public uint MasterEventId { get; set; }
    public EventPointType Type { get; set; }
    public int Amount { get; set; }
    public List<uint> RewardStatus { get; set; } = [];
}
