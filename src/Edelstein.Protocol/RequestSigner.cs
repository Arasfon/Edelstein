using System.Text;

namespace Edelstein.Protocol;

public static class RequestSigner
{
    private const string SecretRequestSignatureKey = "sk1bdzb310n0s9tl";

    public static string SignRequest(long userId, string clientVersion, long timestamp, string jsonData, string privateKey)
    {
        string signatureData = $"{userId}{SecretRequestSignatureKey}{clientVersion}{timestamp}{jsonData}";

        string base64SignatureData = Convert.ToBase64String(Encoding.UTF8.GetBytes(signatureData));

        return Convert.ToBase64String(RsaSigner.SignData(base64SignatureData, Convert.FromBase64String(privateKey)));
    }

    public static bool AuthorizeRequest(long userId, string clientVersion, long timestamp, string jsonData, string signature,
        string userPublicKey)
    {
        string signatureData = $"{userId}{SecretRequestSignatureKey}{clientVersion}{timestamp}{jsonData}";

        string base64SignatureData = Convert.ToBase64String(Encoding.UTF8.GetBytes(signatureData));

        return RsaSigner.VerifySignature(base64SignatureData, Convert.FromBase64String(signature), Convert.FromBase64String(userPublicKey));
    }
}
