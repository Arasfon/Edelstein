using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models;

public class LiveRewardsRetrievalResult
{
    public List<LiveReward> EnsuredRewards { get; set; } = [];
    public List<LiveReward> RandomRewards { get; set; } = [];
}
