using Edelstein.GameServer.Models;

using Microsoft.AspNetCore.Mvc.Filters;

namespace Edelstein.GameServer.Filters;

public class AuthorizeRequestSignatureAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        EncryptedRequest? encryptedRequest = null;

        foreach (object? actionArgument in context.ActionArguments.Values)
        {
            if (actionArgument is not EncryptedRequest encryptedRequestArgument)
                continue;

            encryptedRequest = encryptedRequestArgument;
            break;
        }

        if (encryptedRequest is null)
        {
            base.OnActionExecuting(context);
            return;
        }

        /*if (!Int64.TryParse(context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuUserId], out long userId))
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(GameLibErrorCode.CommonInvalidSignature));
            return;
        }

        if (!Int64.TryParse(context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuTimestamp], out long timestamp))
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(GameLibErrorCode.CommonInvalidSignature));
            return;
        }

        string? clientVersion = context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuClientVersion];

        if (clientVersion is null)
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(GameLibErrorCode.CommonInvalidSignature));
            return;
        }

        string? signature = context.HttpContext.Request.Headers[GameRequestHeaderNames.CurrentRequestSignature];

        if (signature is null)
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(GameLibErrorCode.CommonInvalidSignature));
            return;
        }

        bool isAuthorized =
            RequestSigner.AuthorizeRequest(userId, clientVersion, timestamp, encryptedRequest.DecryptedRequestBody, signature,
                "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMes5UJdSUibcJVG3Nrf53j0ObVFy6XHcCeFSij06d4mQlQ4CFEQ4UNQawM9dPINsYDIm9aipzdBTgNScbqZUJUCAwEAAQ==");

        if (!isAuthorized)
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(GameLibErrorCode.CommonInvalidSignature));
            return;
        }*/

        base.OnActionExecuting(context);
    }
}
