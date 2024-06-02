using Edelstein.Server.ActionResults;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models.Endpoints.Notice;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/api/notice")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class NoticeController : Controller
{
    [Route("reward")]
    public AsyncEncryptedResult Reward() =>
        AsyncEncryptedResult.Create(new NoticeRewardResponseData([]));
}
