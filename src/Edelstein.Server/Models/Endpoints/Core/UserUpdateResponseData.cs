using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Core;

public record UserUpdateResponseData(
    User User,
    List<uint> ClearedMissionIds
);
