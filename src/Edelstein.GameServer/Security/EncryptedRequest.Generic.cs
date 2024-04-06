namespace Edelstein.GameServer.Security;

public class EncryptedRequest<T> : EncryptedRequest
{
    public EncryptedRequest(string encryptedData) : base(encryptedData) =>
        DeserializedObject = DeserializeRequestBody<T>();

    public T DeserializedObject { get; }
}
