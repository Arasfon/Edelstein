using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Core;

public record UserUpdateResponseData(
    User User,
    List<uint> ClearedMissionIds
);
