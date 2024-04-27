using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Story;

public record StoryReadResponseData(
    List<Gift> GiftList,
    List<Reward> RewardList,
    UpdatedValueList UpdatedValueList,
    List<uint> ClearMissionIds
);
