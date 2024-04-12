using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Core;

public record LoginBonusResponseData(
    List<LoginBonus> LoginBonusList,
    ulong StartTime,
    List<uint> ClearMissionIds
);
