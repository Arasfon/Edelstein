using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

using System.Diagnostics.CodeAnalysis;

namespace Edelstein.Server.Models;

public class LiveFinishResult
{
    public LiveFinishResult() { }

    [SetsRequiredMembers]
    public LiveFinishResult(LiveFinishResultStatus status)
    {
        Status = status;
        UpdatedUserData = null!;
        ChangedGem = null!;
        RankingChange = null!;
        EventRankingData = null!;
    }

    public required LiveFinishResultStatus Status { get; init; }
    public Live? FinishedLiveData { get; init; }
    public required UserData UpdatedUserData { get; init; }
    public Gem? ChangedGem { get; init; }
    public List<Item> ChangedItems { get; init; } = [];
    public List<Point> ChangedPoints { get; init; } = [];
    public List<Character> UpdatedCharacters { get; init; } = [];
    public HashSet<uint> ClearedMasterLiveMissionIds { get; init; } = [];
    public IEnumerable<Reward> Rewards { get; init; } = [];
    public IEnumerable<Gift> Gifts { get; init; } = [];
    public List<uint> ClearedMissionIds { get; init; } = [];
    public List<Reward> EventPointRewards { get; init; } = [];
    public required RankingChange RankingChange { get; init; }
    public EventMember? EventMember { get; init; }
    public required EventRankingData EventRankingData { get; init; }
}
