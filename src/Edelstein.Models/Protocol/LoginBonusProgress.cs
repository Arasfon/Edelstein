namespace Edelstein.Models.Protocol;

public class LoginBonusProgress
{
    public uint MasterLoginBonusId { get; set; }
    public List<int> DayCounts { get; set; } = [];
    public long ReceivedDateTime { get; set; }
    public long ExpireDateTime { get; set; }
}
