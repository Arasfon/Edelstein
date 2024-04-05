using Edelstein.PaymentServer.Authorization;
using Edelstein.PaymentServer.Models;
using Edelstein.PaymentServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Edelstein.PaymentServer.Controllers;

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
        Claim? xuidClaim = HttpContext.User.FindFirst("Xuid");

        return Ok(new
        {
            result = "OK",
            x_uid = $"{xuidClaim!.Value}",
            x_app_id = $"{GameAppConstants.XAppId}"
        });
    }

    [Route("authorize")]
    [ServiceFilter<OAuthRsaAuthorizationFilter>]
    public IActionResult Authorize() =>
        Ok(new { result = "OK" });
}

public record AuthInitializeRequestData(
    string DeviceId,
    string Token,
    string Payload
);