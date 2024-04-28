using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models;

public record LotteryDrawResult(
    LotteryDrawResultStatus Status,
    IEnumerable<LotteryItem> LotteryItems,
    UpdatedValueList Updates
);
