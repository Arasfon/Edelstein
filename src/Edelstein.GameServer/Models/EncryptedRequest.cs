using Edelstein.GameServer.ModelBinders;
using Edelstein.Protocol;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Edelstein.GameServer.Models;

[ModelBinder<EncryptedRequestModelBinder>]
public class EncryptedRequest
{
    private readonly byte[] _requestIv;

    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public EncryptedRequest(string encryptedData) =>
        DecryptedRequestBody = DataCryptor.ServerDecrypt(encryptedData, out _requestIv);

    public string DecryptedRequestBody { get; }

    public T DeserializeRequestBody<T>() =>
        JsonSerializer.Deserialize<T>(DecryptedRequestBody, DefaultSerializerOptions)!;

    public string EncryptResponse<T>(ServerResponse<T> response) =>
        DataCryptor.ServerEncrypt(JsonSerializer.Serialize(response, DefaultSerializerOptions), _requestIv);

    public string EncryptNewServerResponse<T>(ErrorCode code, T response) =>
        EncryptResponse(new ServerResponse<T>(code, response));

    public string EncryptNewServerResponse(ErrorCode code) =>
        EncryptResponse(new ServerResponse<object?>(code, null));
}
