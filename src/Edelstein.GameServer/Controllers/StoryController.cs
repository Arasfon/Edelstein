using Edelstein.Data.Models.Components;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models.Endpoints.Story;
using Edelstein.GameServer.Security;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/story")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class StoryController : Controller
{
    [HttpPost]
    [Route("read")]
    public EncryptedResult Read(EncryptedRequest encryptedRequest) =>
        new EncryptedResponse<StoryReadResponseData>(new StoryReadResponseData([], [], new UpdatedValueList(), []));
}
