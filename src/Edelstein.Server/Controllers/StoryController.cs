using Edelstein.Data.Models.Components;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models.Endpoints.Story;
using Edelstein.Server.Security;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api/story")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class StoryController : Controller
{
    [HttpPost]
    [Route("read")]
    public AsyncEncryptedResult Read(EncryptedRequest encryptedRequest) =>
        AsyncEncryptedResult.Create(new StoryReadResponseData([], [], new UpdatedValueList(), []));
}
