namespace Edelstein.Models.Protocol;

public class LiveMission
{
    public uint MasterLiveId { get; set; }
    public List<uint> ClearMasterLiveMissionIds { get; set; } = [];
}
