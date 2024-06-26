using Edelstein.Data.Serialization.Json;
using Edelstein.Security;
using Edelstein.Server.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Edelstein.Server.Security;

[ModelBinder<EncryptedRequestModelBinder>]
public class EncryptedRequest
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new BooleanToIntegerJsonConverter() }
    };

    public string RawRequestBody { get; protected init; }
    public string DecryptedRequestBody { get; protected init; }

    protected EncryptedRequest(string rawRequestBody, string decryptedRequestBody)
    {
        RawRequestBody = rawRequestBody;
        DecryptedRequestBody = decryptedRequestBody;
    }

    public EncryptedRequest(string encryptedData)
    {
        RawRequestBody = encryptedData;
        DecryptedRequestBody = encryptedData is "" ? "" : PayloadCryptor.Decrypt(encryptedData);
    }

    public T DeserializeRequestBody<T>() =>
        JsonSerializer.Deserialize<T>(DecryptedRequestBody, DefaultSerializerOptions)!;
}
