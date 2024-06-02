using Edelstein.Data.Extensions;
using Edelstein.Data.Transport;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Configuration.Assets;
using Edelstein.Server.Models.Endpoints.Start;
using Edelstein.Server.Security;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api/start")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class StartController : Controller
{
    private readonly IUserService _userService;
    private readonly ILotteryService _lotteryService;
    private readonly IOptions<AssetsOptions> _assetsOptions;

    public StartController(IUserService userService, ILotteryService lotteryService, IOptions<AssetsOptions> assetsOptions)
    {
        _userService = userService;
        _lotteryService = lotteryService;
        _assetsOptions = assetsOptions;
    }

    [HttpPost]
    [Route("assetHash")]
    [AllowAnonymous]
    public AsyncEncryptedResult AssetHash(EncryptedRequest<AssetHashRequestData> encryptedRequest)
    {
        AssetHashResponseData responseData =
            HttpContext.Request.Headers[GameRequestHeaderNames.AoharuPlatform].FirstOrDefault()?.StartsWith("Android") == true
                ? new AssetHashResponseData(_assetsOptions.Value.Hashes.Android)
                : new AssetHashResponseData(_assetsOptions.Value.Hashes.Ios);

        return AsyncEncryptedResult.Create(responseData);
    }

    [HttpPost]
    [Route("")]
    public async Task<AsyncEncryptedResult> Start(EncryptedRequest<StartRequestData> encryptedRequest)
    {
        // TODO: token was ae72a65f58f0ff9d50d4bb0b3cfa71d34cfd3f94 some time ago, does it actually do something?
        // TODO: token was ae68f469da9e68ed6ce1f7c983e4c9181538336a even earlier

        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _userService.UpdateLastLoginTime(xuid);

        _ = await _lotteryService.GetAndRefreshUserLotteriesData(xuid);

        const string token = "token";

        StartResponseData responseData =
            HttpContext.Request.Headers[GameRequestHeaderNames.AoharuPlatform].FirstOrDefault()?.StartsWith("Android") == true
                ? new StartResponseData(_assetsOptions.Value.Hashes.Android, token)
                : new StartResponseData(_assetsOptions.Value.Hashes.Ios, token);

        return AsyncEncryptedResult.Create(responseData);
    }

    [Route("refundBalance")]
    public AsyncEncryptedResult RefundBalance() =>
        AsyncEncryptedResult.Create(new RefundBalanceResponseData("0", "0", "0", "payback"));
}
