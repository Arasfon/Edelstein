using Edelstein.GameServer.ModelBinders;
using Edelstein.Protocol;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Edelstein.GameServer.Models;

[ModelBinder<EncryptedRequestModelBinder>]
public class EncryptedRequest
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public EncryptedRequest(string encryptedData) =>
        DecryptedRequestBody = ProtocolCryptor.Decrypt(encryptedData);

    public string DecryptedRequestBody { get; protected init; }

    public T DeserializeRequestBody<T>() =>
        JsonSerializer.Deserialize<T>(DecryptedRequestBody, DefaultSerializerOptions)!;
}
