using System.Security.Cryptography;
using System.Text;

namespace Edelstein.Protocol;

public static class DataSigner
{
    private const string PrivateKey =
        "MIIBVAIBADANBgkqhkiG9w0BAQEFAASCAT4wggE6AgEAAkEAx6zlQl1JSJtwlUbc2t/nePQ5tUXLpcdwJ4VKKPTp3iZCVDgIURDhQ1BrAz108g2xgMib1qKnN0FOA1JxuplQlQIDAQABAkAfy/ZRKGJOY+RcfSWX3C5z6i5TYcsxec4wGO9rkL66P4THAdw8qoEz6ITwyGtBaDcoXqvLliXDeXC5QZ9fDQV5AiEA8bN0RSpHS3FIMdsp37LJVEecWp+KUHZdzGZlaXderA0CIQDTfPdRcU2lJDhd61/mkWZisP/aAv0ms9u7SknbOkWsqQIgDdR85LBEoBZ9UQz/WmN6ja4DTlQur+f1+gop510DN8kCIEvqTsjgWBPQhZ9JD1qvlMYWbTDv47lR1K1NRGe6aipJAiEAlDRODHltMryJZlFrwONcGAIUzYxHFILBI/y767SUl6o=";

    private static readonly byte[] PrivateKeyBytes = Convert.FromBase64String(PrivateKey);

    private const string PublicKey =
        "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMes5UJdSUibcJVG3Nrf53j0ObVFy6XHcCeFSij06d4mQlQ4CFEQ4UNQawM9dPINsYDIm9aipzdBTgNScbqZUJUCAwEAAQ==";

    private static readonly byte[] PublicKeyBytes = Convert.FromBase64String(PublicKey);

    private const string SecretKey = "sk1bdzb310n0s9tl";

    public static byte[] Sign(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(PrivateKeyBytes, out _);

        return rsa.SignData(dataBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }

    public static string SignRequest(long userId, string clientVersion, long timestamp, string jsonData)
    {
        string signatureData = $"{userId}{SecretKey}{clientVersion}{timestamp}{jsonData}";

        string base64SignatureData = Convert.ToBase64String(Encoding.UTF8.GetBytes(signatureData));

        return Convert.ToBase64String(Sign(base64SignatureData));
    }

    public static bool VerifySignature(string data, byte[] signatureBytes)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(PublicKeyBytes, out _);

        return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }

    public static bool VerifyRequestSignature(long userId, string clientVersion, long timestamp, string jsonData, string signature)
    {
        string signatureData = $"{userId}{SecretKey}{clientVersion}{timestamp}{jsonData}";

        string base64SignatureData = Convert.ToBase64String(Encoding.UTF8.GetBytes(signatureData));

        return VerifySignature(base64SignatureData, Convert.FromBase64String(signature));
    }
}
