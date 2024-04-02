using CommunityToolkit.HighPerformance;

using System.Security.Cryptography;
using System.Text;

namespace Edelstein.Protocol;

#pragma warning disable SYSLIB0022
// TODO: Port to AES
public static class ProtocolCryptor
{
    private const string RijndaelKey = "3559b435f24b297a79c68b9709ef2125";
    private static readonly byte[] RijndaelKeyBytes = Encoding.UTF8.GetBytes(RijndaelKey);

    public static string Decrypt(string encryptedData)
    {
        byte[] encryptedDataBytes = Convert.FromBase64String(encryptedData);

        using RijndaelManaged rijndael = new();

        byte[] iv = encryptedDataBytes[..16];

        using ICryptoTransform decryptor = rijndael.CreateDecryptor(RijndaelKeyBytes, iv);

        using CryptoStream cryptoStream = new(encryptedDataBytes.AsMemory(16).AsStream(), decryptor, CryptoStreamMode.Read);
        using MemoryStream decryptedDataStream = new();

        cryptoStream.CopyTo(decryptedDataStream);

        return Encoding.UTF8.GetString(decryptedDataStream.ToArray());
    }

    public static string Encrypt(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RijndaelManaged rijndael = new();
        rijndael.GenerateIV();

        using ICryptoTransform encryptor = rijndael.CreateEncryptor(RijndaelKeyBytes, rijndael.IV);

        using MemoryStream encryptedDataStream = new();
        using CryptoStream cryptoStream = new(encryptedDataStream, encryptor, CryptoStreamMode.Write);

        rijndael.IV.AsMemory().AsStream().CopyTo(encryptedDataStream);
        dataBytes.AsMemory().AsStream().CopyTo(cryptoStream);

        cryptoStream.FlushFinalBlock();

        return Convert.ToBase64String(encryptedDataStream.ToArray());
    }
}
