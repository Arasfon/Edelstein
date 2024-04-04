using Edelstein.GameServer.ModelBinders;
using Edelstein.Security;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Edelstein.GameServer.Encryption;

[ModelBinder<EncryptedRequestModelBinder>]
public class EncryptedRequest
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public string RawRequestBody { get; protected init; }
    public string DecryptedRequestBody { get; protected init; }

    public EncryptedRequest(string encryptedData)
    {
        RawRequestBody = encryptedData;
        DecryptedRequestBody = PayloadCryptor.Decrypt(encryptedData);
    }

    public T DeserializeRequestBody<T>() =>
        JsonSerializer.Deserialize<T>(DecryptedRequestBody, DefaultSerializerOptions)!;
}
