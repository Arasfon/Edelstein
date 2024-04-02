namespace Edelstein.Models.Protocol;

public class Card
{
    public ulong Id { get; set; }
    public uint MasterCardId { get; set; }
    public int Exp { get; set; }
    public int SkillExp { get; set; }
    public List<EvolveInfo> Evolve { get; set; } = [];
    public List<int> Episode { get; set; } = [];
    public long CreatedDateTime { get; set; }
}
