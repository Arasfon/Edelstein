using Edelstein.Data.Models;
using Edelstein.Data.Serialization.Json;
using Edelstein.Data.Transport;
using Edelstein.Server.Models.Endpoints.Chat;
using Edelstein.Server.Models.Endpoints.Core;
using Edelstein.Server.Models.Endpoints.Live;
using Edelstein.Server.Models.Endpoints.Lottery;
using Edelstein.Server.Models.Endpoints.Notice;
using Edelstein.Server.Models.Endpoints.Start;
using Edelstein.Server.Models.Endpoints.Story;

using System.Text.Json.Serialization;

namespace Edelstein.Server.Models.Serializers;

[JsonSourceGenerationOptions(Converters =
    [
        typeof(BooleanToIntegerJsonConverter),
        typeof(OneOfListOrObjectJsonConverterFactory)
    ],
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ServerResponse<AssetHashResponseData>))]
[JsonSerializable(typeof(ServerResponse<ChatHomeResponseData>))]
[JsonSerializable(typeof(ServerResponse<ChatTalkStartResponseData>))]
[JsonSerializable(typeof(ServerResponse<DeckResponseData>))]
[JsonSerializable(typeof(ServerResponse<DrawLotteryResponseData>))]
[JsonSerializable(typeof(ServerResponse<FriendResponseData>))]
[JsonSerializable(typeof(ServerResponse<GetLotteriesResponseData>))]
[JsonSerializable(typeof(ServerResponse<GiftResponseData>))]
[JsonSerializable(typeof(ServerResponse<HomeResponseData>))]
[JsonSerializable(typeof(ServerResponse<HomeResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveClearRateResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveContinueResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveEndResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveGuestResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveMissionResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveRankingResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveRetireResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveRewardsResponseData>))]
[JsonSerializable(typeof(ServerResponse<LiveSkipResponseData>))]
[JsonSerializable(typeof(ServerResponse<LoginBonusResponseData>))]
[JsonSerializable(typeof(ServerResponse<LotteryTutorialResponseData>))]
[JsonSerializable(typeof(ServerResponse<NoticeRewardResponseData>))]
[JsonSerializable(typeof(ServerResponse<RefundBalanceResponseData>))]
[JsonSerializable(typeof(ServerResponse<StartResponseData>))]
[JsonSerializable(typeof(ServerResponse<StoryReadResponseData>))]
[JsonSerializable(typeof(ServerResponse<UserUpdateResponseData>))]
[JsonSerializable(typeof(ServerResponse<UserData>))]
[JsonSerializable(typeof(ServerResponse<UserMissionsDocument>))]
public partial class ServerResponseJsonSerializerContext : JsonSerializerContext;
