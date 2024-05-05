using Edelstein.Data.Transport;
using Edelstein.Security;
using Edelstein.Server.Configuration.OAuth;
using Edelstein.Server.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Edelstein.Server.Authorization;

public class OAuthHmacAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IOptions<OAuthOptions> _oauthOptions;

    public OAuthHmacAuthorizationFilter(IOptions<OAuthOptions> oauthOptions) =>
        _oauthOptions = oauthOptions;

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

        // Actual authentication
        bool verified = OAuth.VerifyOAuthHmac(httpMethod, url, body, requestOAuthValues);

        if (!verified)
        {
            context.Result =
                new ObjectResult(new GameLibErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
        }
    }
}
