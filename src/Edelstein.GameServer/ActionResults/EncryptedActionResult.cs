using Edelstein.GameServer.Models;
using Edelstein.Models.Protocol;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.ActionResults;

public class EncryptedActionResult : IActionResult
{
    private readonly int _statusCode;
    private readonly EncryptedResponse _encryptedResponse;

    public EncryptedActionResult(int statusCode, EncryptedResponse encryptedResponse)
    {
        _statusCode = statusCode;
        _encryptedResponse = encryptedResponse;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        string? encryptedString = _encryptedResponse.EncryptedString;

        ContentResult contentResult = new()
        {
            Content = encryptedString,
            ContentType = "application/json",
            StatusCode = _statusCode
        };

        await contentResult.ExecuteResultAsync(context);
    }

    public static implicit operator EncryptedActionResult(EncryptedResponse response) =>
        new(StatusCodes.Status200OK, response);
}
