using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveRewardsResponseData(
    List<LiveReward> EnsuredList,
    List<LiveReward> RandomList
);
