using Edelstein.Data.Transport;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Security;
using Edelstein.GameServer.Models.Endpoints.Start;

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
    [ServiceFilter<RsaSignatureAuthorizationFilter>]
    public EncryptedResult Start(EncryptedRequest<StartRequestData> encryptedRequest)
    {
        // TODO: token was ae72a65f58f0ff9d50d4bb0b3cfa71d34cfd3f94 some time ago, does it actually do something?
        // TODO: token was ae68f469da9e68ed6ce1f7c983e4c9181538336a even earlier

        const string token = "token";

        StartResponseData responseData =
            HttpContext.Request.Headers[GameRequestHeaderNames.AoharuPlatform].FirstOrDefault()?.StartsWith("Android") == true
                ? new StartResponseData(AndroidAssetHash, token)
                : new StartResponseData(IosAssetHash, token);

        return new EncryptedResponse<StartResponseData>(responseData);
    }

    [Route("refundBalance")]
    [ServiceFilter<RsaSignatureAuthorizationFilter>]
    public EncryptedResult RefundBalance() =>
        new EncryptedResponse<RefundBalanceResponseData>(new RefundBalanceResponseData("0", "0", "0", "payback"));
}
