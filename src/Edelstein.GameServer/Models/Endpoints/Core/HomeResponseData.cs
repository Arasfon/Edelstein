using Edelstein.Data.Models;

namespace Edelstein.GameServer.Models.Endpoints.Core;

public record HomeResponseData(
    Home Home,
    List<uint> ClearMissionIds
);
