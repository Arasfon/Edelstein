using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Story;

public record StoryReadResponseData(
    List<Gift> GiftList,
    List<Reward> RewardList,
    UpdatedValueList UpdatedValueList,
    List<uint> ClearMissionIds
);
