using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models;

public class LiveFinishResult
{
    public Live? PreviousLiveData { get; init; }
    public required UserData UpdatedUserData { get; init; }
    public required Gem ChangedGem { get; init; }
    public List<Item> ChangedItems { get; init; } = [];
    public List<Point> ChangedPoints { get; init; } = [];
    public List<Character> DeckCharacters { get; init; } = [];
    public List<uint> ClearedMasterLiveMissionIds { get; init; } = [];
    public List<Reward> Rewards { get; init; } = [];
    public List<Gift> NewGifts { get; init; } = [];
    public List<uint> ClearedMissionIds { get; init; } = [];
    public List<Reward> EventPointRewards { get; init; } = [];
    public required RankingChange RankingChange { get; init; }
    public EventMember? EventMember { get; init; }
    public required EventRankingData EventRankingData { get; init; }
}
