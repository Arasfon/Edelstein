using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models;

public record GiftClaimResult(
    IEnumerable<ulong> FailedGifts,
    UpdatedValueList UpdatedValueList,
    List<Reward> Rewards
);
