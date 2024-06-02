using Edelstein.Data.Models;
using Edelstein.Data.Transport;
using Edelstein.Security;
using Edelstein.Server.ActionResults;
using Edelstein.Server.Security;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

using System.Security.Claims;

namespace Edelstein.Server.Authorization;

public class RsaSignatureAuthorizationFilter : IAsyncAuthorizationFilter
{
    public const string ComputedEncryptedRequestItemName = "AuthComputedEncryptedRequest";
    public const int TimeoutSeconds = 30;

    private readonly IUserService _userService;

    public RsaSignatureAuthorizationFilter(IUserService userService) =>
        _userService = userService;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (hasAllowAnonymous)
            return;

        // Setup
        DateTimeOffset now = DateTimeOffset.UtcNow;

        HttpContext httpContext = context.HttpContext;

        if (!httpContext.Request.Body.CanSeek)
            httpContext.Request.EnableBuffering();

        string body;
        httpContext.Request.Body.Position = 0;
        using (StreamReader sr = new(httpContext.Request.Body, leaveOpen: true))
        {
            body = await sr.ReadToEndAsync();
        }

        httpContext.Request.Body.Position = 0;

        EncryptedRequest encryptedRequest = new(body);
        httpContext.Items[ComputedEncryptedRequestItemName] = encryptedRequest;

        // Header verification
        if (!context.HttpContext.Request.Headers.TryGetValue(GameRequestHeaderNames.AoharuUserId, out StringValues xuidStringValues) ||
            !UInt64.TryParse(xuidStringValues, out ulong xuid))
        {
            context.Result = AsyncEncryptedResult.Create(StatusCodes.Status400BadRequest, ErrorCode.ErrorBadRequest);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(GameRequestHeaderNames.AoharuTimestamp,
                out StringValues timestampStringValues) ||
            !Int64.TryParse(timestampStringValues, out long timestamp) ||
            timestamp > (now + TimeSpan.FromSeconds(TimeoutSeconds)).ToUnixTimeSeconds() ||
            timestamp < (now - TimeSpan.FromSeconds(TimeoutSeconds)).ToUnixTimeSeconds())
        {
            context.Result = AsyncEncryptedResult.Create(StatusCodes.Status400BadRequest, ErrorCode.ErrorBadRequest);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(GameRequestHeaderNames.AoharuClientVersion,
            out StringValues clientVersionStringValues))
        {
            context.Result = AsyncEncryptedResult.Create(StatusCodes.Status400BadRequest, ErrorCode.ErrorBadRequest);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(GameRequestHeaderNames.CurrentRequestSignature,
            out StringValues signatureStringValues))
        {
            context.Result = AsyncEncryptedResult.Create(StatusCodes.Status400BadRequest, ErrorCode.ErrorBadRequest);
            return;
        }

        // Authentication data retrieval
        AuthenticationData? userAuthenticationData = await _userService.GetAuthenticationDataByXuid(xuid);

        if (userAuthenticationData is null)
        {
            context.Result = AsyncEncryptedResult.Create(StatusCodes.Status400BadRequest, ErrorCode.ErrorUserNotFound);
            return;
        }

        // Authorization
        bool isAuthorized =
            RequestSigner.AuthorizeRequest(xuid, clientVersionStringValues!, timestamp, encryptedRequest.DecryptedRequestBody,
                signatureStringValues!, userAuthenticationData.PublicKey);

        if (!isAuthorized)
        {
            context.Result = AsyncEncryptedResult.Create(StatusCodes.Status400BadRequest, ErrorCode.ErrorUnauthorized);
            return;
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimNames.UserId, userAuthenticationData.UserIdString),
            new Claim(ClaimNames.Xuid, userAuthenticationData.Xuid.ToString())
        ]));
    }
}
