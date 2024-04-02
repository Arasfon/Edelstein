namespace Edelstein.Models.Protocol;

public class Membership
{
    public int MembershipNumberId { get; set; }
    public uint MasterMembershipWallpaperId { get; set; }
    public int TotalPurchaseCount { get; set; }
    public required LoginBonusProgress LoginBonusList { get; set; }
    public long MembershipStartDateTime { get; set; }
    public long LastPurchaseDateTime { get; set; }
    public long ExpireDateTime { get; set; }
}
