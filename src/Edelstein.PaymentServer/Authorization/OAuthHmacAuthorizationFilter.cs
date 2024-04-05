using Edelstein.Data.Transport;
using Edelstein.PaymentServer.Configuration.OAuth;
using Edelstein.PaymentServer.Models;
using Edelstein.Security;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Edelstein.PaymentServer.Authorization;

public class OAuthHmacAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly OAuthOptions _oauthOptions;

    public OAuthHmacAuthorizationFilter(IOptions<OAuthOptions> oauthOptions) =>
        _oauthOptions = oauthOptions.Value;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Setup
        HttpContext httpContext = context.HttpContext;

        if (!httpContext.Request.Body.CanSeek)
            httpContext.Request.EnableBuffering();

        string httpMethod = httpContext.Request.Method;
        string url = httpContext.Request.GetDisplayUrl();
        string requestAuthorizationHeader = httpContext.Request.Headers.Authorization!;

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
        bool verified = OAuth.VerifyOAuthHmac(httpMethod, url, body, requestOAuthValues);

        if (!verified)
        {
            context.Result =
                new ObjectResult(new ErrorResponseData(GameLibErrorCode.CommonInvalidSignature, "Invalid Signature", "NG"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
        }
    }
}