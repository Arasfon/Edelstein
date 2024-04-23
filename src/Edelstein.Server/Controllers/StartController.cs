using Edelstein.Data.Extensions;
using Edelstein.Data.Transport;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models.Endpoints.Start;
using Edelstein.Server.Security;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api/start")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class StartController : Controller
{
    private const string AndroidAssetHash = "67f8f261c16b3cca63e520a25aad6c1c";
    private const string IosAssetHash = "b8975be8300013a168d061d3fdcd4a16";

    private readonly IUserService _userService;
    private readonly ILotteryService _lotteryService;

    public StartController(IUserService userService, ILotteryService lotteryService)
    {
        _userService = userService;
        _lotteryService = lotteryService;
    }

    [HttpPost]
    [Route("assetHash")]
    [AllowAnonymous]
    public EncryptedResult AssetHash(EncryptedRequest<AssetHashRequestData> encryptedRequest)
    {
        AssetHashResponseData responseData =
            HttpContext.Request.Headers[GameRequestHeaderNames.AoharuPlatform].FirstOrDefault()?.StartsWith("Android") == true
                ? new AssetHashResponseData(AndroidAssetHash)
                : new AssetHashResponseData(IosAssetHash);

        return new EncryptedResponse<AssetHashResponseData>(responseData);
    }

    [HttpPost]
    [Route("")]
    public async Task<EncryptedResult> Start(EncryptedRequest<StartRequestData> encryptedRequest)
    {
        // TODO: token was ae72a65f58f0ff9d50d4bb0b3cfa71d34cfd3f94 some time ago, does it actually do something?
        // TODO: token was ae68f469da9e68ed6ce1f7c983e4c9181538336a even earlier

        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _userService.UpdateLastLoginTime(xuid);

        _ = await _lotteryService.GetAndRefreshUserLotteriesData(xuid);

        const string token = "token";

        StartResponseData responseData =
            HttpContext.Request.Headers[GameRequestHeaderNames.AoharuPlatform].FirstOrDefault()?.StartsWith("Android") == true
                ? new StartResponseData(AndroidAssetHash, token)
                : new StartResponseData(IosAssetHash, token);

        return new EncryptedResponse<StartResponseData>(responseData);
    }

    [Route("refundBalance")]
    public EncryptedResult RefundBalance() =>
        new EncryptedResponse<RefundBalanceResponseData>(new RefundBalanceResponseData("0", "0", "0", "payback"));
}
