namespace Edelstein.Models.Protocol;

public class Live
{
    public uint MasterLiveId { get; set; }
    public int Level { get; set; }
    public int ClearCount { get; set; }
    public int HighScore { get; set; }
    public int MaxCombo { get; set; }
    public bool AutoEnable { get; set; }
    public int UpdatedTime { get; set; }
}
