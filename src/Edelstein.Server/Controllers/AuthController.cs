using Edelstein.Data.Transport;
using Edelstein.Server.Authorization;
using Edelstein.Server.Models;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.Server.Controllers
{
    [ApiController]
    [Route("/v1.0/auth")]
    public class AuthController : Controller
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService) =>
            _userService = userService;

        [HttpPost]
        [Route("initialize")]
        [ServiceFilter<OAuthHmacAuthorizationFilter>]
        public async Task<IActionResult> Initialize(AuthInitializeRequestData authInitializeRequestData)
        {
            string publicKey = authInitializeRequestData.Token
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "");

            UserRegistrationResult registrationResult = await _userService.RegisterUser(publicKey);

            return Ok(new
            {
                result = "OK",
                app_id = $"{GameAppConstants.AppId}",
                uuid = $"{GameAppConstants.AppId}{registrationResult.AuthenticationData.UserIdString}"
            });
        }

        [Route("x_uid")]
        [ServiceFilter<OAuthRsaAuthorizationFilter>]
        public IActionResult Xuid()
        {
            string? xuidString = User.FindFirst(ClaimNames.Xuid)?.Value;

            return Ok(new
            {
                result = "OK",
                x_uid = $"{xuidString}",
                x_app_id = $"{GameAppConstants.XAppId}"
            });
        }

        [Route("authorize")]
        [ServiceFilter<OAuthRsaAuthorizationFilter>]
        public IActionResult Authorize() =>
            Ok(new { result = "OK" });
    }
}

public record AuthInitializeRequestData(
    string DeviceId,
    string Token,
    string Payload
);
