using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models;

public record LotteryDrawResult(
    LotteryDrawResultStatus Status,
    List<LotteryItem> LotteryItems,
    UpdatedValueList Updates
);
