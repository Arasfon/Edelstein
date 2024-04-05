using Edelstein.Data.Transport;
using Edelstein.PaymentServer.Configuration.OAuth;
using Edelstein.PaymentServer.Models;
using Edelstein.PaymentServer.Services;
using Edelstein.Security;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

using System.Security.Claims;

namespace Edelstein.PaymentServer.Authorization;

public class OAuthRsaAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly OAuthOptions _oauthOptions;
    private readonly IUserService _userService;

    public OAuthRsaAuthorizationFilter(IOptions<OAuthOptions> oauthOptions, IUserService userService)
    {
        _oauthOptions = oauthOptions.Value;
        _userService = userService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Setup
        HttpContext httpContext = context.HttpContext;

        if (!httpContext.Request.Body.CanSeek)
            httpContext.Request.EnableBuffering();

        string httpMethod = httpContext.Request.Method;
        string url = httpContext.Request.GetDisplayUrl();
        string? requestAuthorizationHeader = httpContext.Request.Headers.Authorization;

        if (requestAuthorizationHeader is null)
        {
            context.Result =
                new ObjectResult(new ErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            return;
        }

        string body;
        httpContext.Request.Body.Position = 0;
        using (StreamReader sr = new(httpContext.Request.Body, leaveOpen: true))
        {
            body = await sr.ReadToEndAsync();
        }

        httpContext.Request.Body.Position = 0;

        // Replace url host e.g. if behind reverse-proxy
        if (_oauthOptions.SigningUrlHost is { ShouldReplace: true, Replacement: not null })
            url = url.Replace(httpContext.Request.Host.Value, _oauthOptions.SigningUrlHost.Replacement);

        // Make values
        Dictionary<string, string> requestOAuthValues = OAuth.BuildOAuthValuesFromHeader(requestAuthorizationHeader);

        // Make response header
        string responseOAuth = OAuth.BuildResponseOAuthHeader(httpMethod, url, requestOAuthValues["oauth_body_hash"]);
        context.HttpContext.Response.Headers["X-GREE-Authorization"] = responseOAuth;

        // Actual authentication
        if (!OAuth.TryGetUserIdFromRequestorId(requestOAuthValues["xoauth_requestor_id"], out string? userId))
        {
            context.Result =
                new ObjectResult(new ErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        AuthenticationData? userAuthenticationData = await _userService.GetAuthenticationDataByUserId(Guid.Parse(userId));

        if (userAuthenticationData is null)
        {
            context.Result =
                new ObjectResult(new ErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        string publicKey = userAuthenticationData.PublicKey;

        bool verified = OAuth.VerifyOAuthRsa(httpMethod, url, body, requestOAuthValues, publicKey);

        if (!verified)
        {
            context.Result =
                new ObjectResult(new ErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("UserId", userAuthenticationData.UserIdString),
            new Claim("Xuid", userAuthenticationData.Xuid.ToString())
        ]));
    }
}
