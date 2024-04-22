using Edelstein.Data.Models;

namespace Edelstein.Server.Models.Endpoints.Core;

public record HomeResponseData(
    Home Home,
    List<uint> ClearMissionIds
);
