namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveMissionResponseData(
    string ScoreRanking,
    string ComboRanking,
    string ClearCountRanking
);
