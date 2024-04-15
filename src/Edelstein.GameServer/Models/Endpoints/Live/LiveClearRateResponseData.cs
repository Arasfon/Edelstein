using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveClearRateResponseData(
    List<AllUserClearRate> AllUserClearRate,
    IEnumerable<uint> MasterMusicIds,
    List<EventLiveData> EventLiveList
);
