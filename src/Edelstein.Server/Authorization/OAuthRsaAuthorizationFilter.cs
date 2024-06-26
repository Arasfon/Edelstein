using Edelstein.Data.Models;
using Edelstein.Data.Transport;
using Edelstein.Security;
using Edelstein.Server.Configuration.OAuth;
using Edelstein.Server.Models;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

using System.Security.Claims;

namespace Edelstein.Server.Authorization;

public class OAuthRsaAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IOptions<OAuthOptions> _oauthOptions;
    private readonly IUserService _userService;

    public OAuthRsaAuthorizationFilter(IOptions<OAuthOptions> oauthOptions, IUserService userService)
    {
        _oauthOptions = oauthOptions;
        _userService = userService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (hasAllowAnonymous)
            return;

        OAuthOptions oauthOptions = _oauthOptions.Value;

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
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
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
        if (oauthOptions.SigningUrlHost is { ShouldReplace: true, Replacement: not null })
            url = url.Replace(httpContext.Request.Host.Value, oauthOptions.SigningUrlHost.Replacement);

        // Make values
        Dictionary<string, string> requestOAuthValues = OAuth.BuildOAuthValuesFromHeader(requestAuthorizationHeader);

        if (!requestOAuthValues.TryGetValue("oauth_body_hash", out string? oauthBodyHash))
        {
            context.Result =
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        // Make response header
        string responseOAuth = OAuth.BuildResponseOAuthHeader(httpMethod, url, oauthBodyHash);
        context.HttpContext.Response.Headers["X-GREE-Authorization"] = responseOAuth;

        // Authentication data retrieval
        if (!requestOAuthValues.TryGetValue("xoauth_requestor_id", out string? xoauthRequestorId))
        {
            context.Result =
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        if (!OAuth.TryGetUserIdFromRequestorId(xoauthRequestorId, out string? userId))
        {
            context.Result =
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        AuthenticationData? userAuthenticationData = await _userService.GetAuthenticationDataByUserId(Guid.Parse(userId));

        if (userAuthenticationData is null)
        {
            context.Result =
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        string publicKey = userAuthenticationData.PublicKey;

        // Authorization
        bool verified = OAuth.VerifyOAuthRsa(httpMethod, url, body, requestOAuthValues, publicKey);

        if (!verified)
        {
            context.Result =
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

            return;
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimNames.UserId, userAuthenticationData.UserIdString),
            new Claim(ClaimNames.Xuid, userAuthenticationData.Xuid.ToString())
        ]));
    }
}
