namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveMissionResponseData(
    string ScoreRanking,
    string ComboRanking,
    string ClearCountRanking
);
