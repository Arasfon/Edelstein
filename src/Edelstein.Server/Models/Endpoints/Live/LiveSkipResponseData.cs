using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Live;

public class LiveSkipResponseData
{
    public Data.Models.Components.Live? Live { get; set; }
    public List<uint> ClearMasterLiveMissionIds { get; set; } = [];
    public required User User { get; set; }
    public required Stamina Stamina { get; set; }
    public List<Character> CharacterList { get; set; } = [];
    public List<Card> CardList { get; set; } = [];
    public List<CardSub> CardSubList { get; set; } = [];
    public List<Item> ItemList { get; set; } = [];
    public List<Point> PointList { get; set; } = [];
    public List<Group> GroupList { get; set; } = [];
    public List<Reward> RewardList { get; set; } = [];
    public List<Gift> GiftList { get; set; } = [];
    public List<uint> ClearMissionIds { get; set; } = [];
    public List<EventPoint> EventPointList { get; set; } = [];
    public List<Reward> EventPointRewardList { get; set; } = [];
    public required RankingChange RankingChange { get; set; }
    public List<Reward> MusicMissionRewardList { get; set; } = [];
    public required EventRankingData EventRankingData { get; set; }
    public StarEventBonus? StarEventBonusList { get; set; }
    public uint TotalScore { get; set; }
    public StarEventData? StarEvent { get; set; }
}
