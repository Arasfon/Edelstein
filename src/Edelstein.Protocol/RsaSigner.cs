using System.Security.Cryptography;
using System.Text;

namespace Edelstein.Protocol;

public static class RsaSigner
{
    public static byte[] SignData(string data, byte[] privateKey)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKey, out _);

        return rsa.SignData(dataBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }

    public static bool VerifySignature(string data, byte[] signature, byte[] publicKey)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey, out _);

        return rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }
}
