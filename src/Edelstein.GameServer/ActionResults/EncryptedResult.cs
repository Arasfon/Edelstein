using Edelstein.GameServer.Security;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.ActionResults;

public class EncryptedResult : IActionResult
{
    private readonly int _statusCode;
    private readonly EncryptedResponse _encryptedResponse;

    public EncryptedResult(int statusCode, EncryptedResponse encryptedResponse)
    {
        _statusCode = statusCode;
        _encryptedResponse = encryptedResponse;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        string encryptedString = _encryptedResponse.EncryptedString;

        ContentResult contentResult = new()
        {
            Content = encryptedString,
            ContentType = "application/json",
            StatusCode = _statusCode
        };

        await contentResult.ExecuteResultAsync(context);
    }

    public static implicit operator EncryptedResult(EncryptedResponse response) =>
        new(StatusCodes.Status200OK, response);
}
