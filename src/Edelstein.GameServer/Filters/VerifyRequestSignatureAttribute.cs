using Edelstein.GameServer.Models;
using Edelstein.Protocol;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Edelstein.GameServer.Filters;

public class VerifyRequestSignatureAttribute : ActionFilterAttribute
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

        if (context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuUserId] +
            context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuTimestamp] !=
            context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuSid])
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(ErrorCode.CommonInvalidParameter));
            return;
        }

        if (!Int64.TryParse(context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuUserId], out long userId))
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(ErrorCode.CommonInvalidSignature));
            return;
        }

        if (!Int64.TryParse(context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuTimestamp], out long timestamp))
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(ErrorCode.CommonInvalidSignature));
            return;
        }

        string? clientVersion = context.HttpContext.Request.Headers[GameRequestHeaderNames.AoharuClientVersion];

        if (clientVersion is null)
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(ErrorCode.CommonInvalidSignature));
            return;
        }

        string? signature = context.HttpContext.Request.Headers[GameRequestHeaderNames.CurrentRequestSignature];

        if (signature is null)
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(ErrorCode.CommonInvalidSignature));
            return;
        }

        bool isVerified =
            DataSigner.VerifyRequestSignature(userId, clientVersion, timestamp, encryptedRequest.DecryptedRequestBody, signature);

        if (!isVerified)
        {
            context.Result = new BadRequestObjectResult(encryptedRequest.EncryptNewServerResponse(ErrorCode.CommonInvalidSignature));
            return;
        }

        base.OnActionExecuting(context);
    }
}
