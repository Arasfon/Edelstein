using CommunityToolkit.HighPerformance;

using System.Security.Cryptography;
using System.Text;

namespace Edelstein.Protocol;

#pragma warning disable SYSLIB0022
public static class DataCryptor
{
    private const string RijndaelKey = "3559b435f24b297a79c68b9709ef2125";
    private static readonly byte[] RijndaelKeyBytes = Encoding.UTF8.GetBytes(RijndaelKey);

    public static string ServerDecrypt(string encryptedData, out byte[] iv)
    {
        byte[] encryptedDataBytes = Convert.FromBase64String(encryptedData);

        using RijndaelManaged rijndael = new();

        iv = encryptedDataBytes[..16];
        rijndael.IV = iv;

        using ICryptoTransform decryptor = rijndael.CreateDecryptor(RijndaelKeyBytes, rijndael.IV);

        using CryptoStream cryptoStream = new(encryptedDataBytes.AsMemory(16).AsStream(), decryptor, CryptoStreamMode.Read);
        using MemoryStream decryptedDataStream = new();

        cryptoStream.CopyTo(decryptedDataStream);

        return Encoding.UTF8.GetString(decryptedDataStream.ToArray());
    }

    public static string ClientDecrypt(string encryptedData, byte[] iv)
    {
        byte[] encryptedDataBytes = Convert.FromBase64String(encryptedData);

        using RijndaelManaged rijndael = new();

        rijndael.IV = iv;

        using ICryptoTransform decryptor = rijndael.CreateDecryptor(RijndaelKeyBytes, rijndael.IV);

        using CryptoStream cryptoStream = new(encryptedDataBytes.AsMemory().AsStream(), decryptor, CryptoStreamMode.Read);
        using MemoryStream decryptedDataStream = new();

        cryptoStream.CopyTo(decryptedDataStream);

        return Encoding.UTF8.GetString(decryptedDataStream.ToArray()[16..]);
    }

    public static string ServerEncrypt(string data, byte[] iv)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RijndaelManaged rijndael = new();

        rijndael.IV = iv;

        using ICryptoTransform encryptor = rijndael.CreateEncryptor(RijndaelKeyBytes, rijndael.IV);

        using MemoryStream encryptedDataStream = new();
        using CryptoStream cryptoStream = new(encryptedDataStream, encryptor, CryptoStreamMode.Write);

        rijndael.IV.AsMemory().AsStream().CopyTo(encryptedDataStream);
        dataBytes.AsMemory().AsStream().CopyTo(cryptoStream);

        cryptoStream.FlushFinalBlock();

        return Convert.ToBase64String(encryptedDataStream.ToArray());
    }

    public static string ClientEncrypt(string data, out byte[] iv)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using RijndaelManaged rijndael = new();

        rijndael.GenerateIV();

        iv = rijndael.IV;

        using ICryptoTransform encryptor = rijndael.CreateEncryptor(RijndaelKeyBytes, rijndael.IV);

        using MemoryStream encryptedDataStream = new();
        using CryptoStream cryptoStream = new(encryptedDataStream, encryptor, CryptoStreamMode.Write);

        rijndael.IV.AsMemory().AsStream().CopyTo(encryptedDataStream);
        dataBytes.AsMemory().AsStream().CopyTo(cryptoStream);

        cryptoStream.FlushFinalBlock();

        return Convert.ToBase64String(encryptedDataStream.ToArray());
    }
}
