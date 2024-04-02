using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Models;
using Edelstein.GameServer.Models.Start;
using Edelstein.Models.Protocol;
using Edelstein.Protocol;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/start")]
public class StartController : Controller
{
    private const string AndroidAssetHash = "67f8f261c16b3cca63e520a25aad6c1c";
    private const string IosAssetHash = "b8975be8300013a168d061d3fdcd4a16";

    [HttpPost]
    [Route("assetHash")]
    public EncryptedActionResult AssetHash(EncryptedRequest<AssetHashRequest> encryptedRequest)
    {
        AssetHashResponse response =
            HttpContext.Request.Headers[GameRequestHeaderNames.AoharuPlatform].FirstOrDefault()?.StartsWith("Android") == true
                ? new AssetHashResponse(AndroidAssetHash)
                : new AssetHashResponse(IosAssetHash);

        return new EncryptedResponse<AssetHashResponse>(ErrorCode.Success, response);
    }

    [HttpPost]
    [Route("")]
    public EncryptedActionResult Start(EncryptedRequest<StartRequest> encryptedRequest) =>
        // TODO: token was ae72a65f58f0ff9d50d4bb0b3cfa71d34cfd3f94 some time ago, does it actually do something?
        // TODO: Check platform
        new EncryptedResponse<StartResponse>(ErrorCode.Success, new StartResponse(AndroidAssetHash, "token"));

    [HttpGet]
    [Route("refundBalance")]
    public EncryptedActionResult RefundBalance() =>
        new EncryptedResponse<RefundBalanceResponse>(ErrorCode.Success, new RefundBalanceResponse("0", "0", "0", "payback"));
}
