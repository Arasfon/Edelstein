using Edelstein.GameServer.Models;
using Edelstein.GameServer.Models.Start;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/start")]
public class StartController : Controller
{
    [HttpPost]
    [Route("assetHash")]
    public ActionResult<string> AssetHash(EncryptedRequest encryptedRequest)
    {
        AssetHashRequest requestData = encryptedRequest.DeserializeRequestBody<AssetHashRequest>();

        return encryptedRequest.EncryptNewServerResponse(0, new AssetHashResponse("5f71eb3c8d3a700c4b89550dabf1ed2f"));
    }
}
