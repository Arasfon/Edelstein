using System.Security.Cryptography;
using System.Text;

namespace Edelstein.Protocol;

public static class HmacSigner
{
    public static byte[] SignData(string data, string secretKey)
    {
        byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using HMACSHA1 hmacsha1 = new HMACSHA1(secretKeyBytes);

        return hmacsha1.ComputeHash(dataBytes);
    }
}
