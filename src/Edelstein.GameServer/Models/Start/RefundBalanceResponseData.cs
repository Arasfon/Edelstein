namespace Edelstein.GameServer.Models.Start;

public record RefundBalanceResponseData(
    string BalanceChargeGem,
    string BalanceFreeGem,
    string BalanceTotalGem,
    string PaybackCode
);
