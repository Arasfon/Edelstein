using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Core;

public record GiftResponseData(IEnumerable<ulong> FailedGiftIds, UpdatedValueList UpdatedValueList, IEnumerable<Reward> RewardList, IEnumerable<uint> ClearMissionIds);
