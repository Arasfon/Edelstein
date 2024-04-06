namespace Edelstein.GameServer.Models.Endpoints.Start;

public record RefundBalanceResponseData(
    string BalanceChargeGem,
    string BalanceFreeGem,
    string BalanceTotalGem,
    string PaybackCode
);
