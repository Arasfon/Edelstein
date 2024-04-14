using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models.Endpoints.Notice;
using Edelstein.GameServer.Security;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/notice")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class NoticeController : Controller
{
    [Route("reward")]
    public EncryptedResult Reward() =>
        new EncryptedResponse<NoticeRewardResponseData>(new NoticeRewardResponseData([]));
}
