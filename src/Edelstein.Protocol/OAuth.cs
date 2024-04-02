using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Edelstein.Protocol;

public static partial class OAuth
{
    private const string OAuthVersion = "1.0";
    private const string OAuthHmacSignatureMethod = "HMAC-SHA1";
    private const string OAuthRsaSignatureMethod = "RSA-SHA1";
    private const string OAuthConsumerKey = "232610769078541";

    private const string SecretHmacKey = "d8f68e284efdf60bbaec2d225c0cfd42";
    private static readonly byte[] SecretHmacKeyBytes = Encoding.UTF8.GetBytes(SecretHmacKey);

    private const int OAuthTimeoutSeconds = 30;

    [GeneratedRegex("(.+)=\"(.+)\"")]
    private static partial Regex OAuthHeaderValueRegex();

    public static string BuildStringToSign(string httpMethod, string url, Dictionary<string, string> oauthValues)
    {
        string encodedOauthValues = String.Join("&", oauthValues.OrderBy(x => x.Key).Aggregate(new List<string>(), (result, pair) =>
        {
            if (pair.Key == "oauth_signature")
                return result;

            result.Add($"{pair.Key}={Uri.EscapeDataString(pair.Value)}");
            return result;
        }));

        return String.Join("&", new List<string>
        {
            httpMethod,
            url,
            encodedOauthValues
        }.Select(Uri.EscapeDataString));
    }

    public static void AppendSignature(string httpMethod, string url, Dictionary<string, string> oauthValues)
    {
        string stringToSign = BuildStringToSign(httpMethod, url, oauthValues);
        string signature = Convert.ToBase64String(HmacSigner.SignData(stringToSign, SecretHmacKey));
        oauthValues["oauth_signature"] = signature;
    }

    public static string BuildOAuthHeader(Dictionary<string, string> oauthValues) =>
        "OAuth " + String.Join(",", oauthValues.OrderBy(x => x.Key).Select(x => $"{x.Key}=\"{Uri.EscapeDataString(x.Value)}\""));

    public static Dictionary<string, string> BuildOAuthValuesFromHeader(string header)
    {
        header = header[6..];

        Dictionary<string, string> values = new();

        foreach (string headerPart in header.Split(","))
        {
            Match match = OAuthHeaderValueRegex().Match(headerPart);
            if (!match.Success)
                continue;

            values.TryAdd(match.Groups[1].Value, Uri.UnescapeDataString(match.Groups[2].Value));
        }

        return values;
    }

    // ReSharper disable once InconsistentNaming
    public static bool TryGetUserIdFromXOAuth(string xoauthRequestorId, [NotNullWhen(true)] out string? userId)
    {
        userId = null;

        if (!xoauthRequestorId.StartsWith(OAuthConsumerKey))
            return false;

        userId = xoauthRequestorId[OAuthConsumerKey.Length..];

        return true;
    }

    public static bool VerifyOAuthHmac(string httpMethod, string url, string body, Dictionary<string, string> oauthValues)
    {
        if (!oauthValues.TryGetValue("oauth_version", out string? oauthVersion) ||
            oauthVersion != OAuthVersion)
            return false;

        if (!oauthValues.TryGetValue("oauth_signature_method", out string? oauthSignatureMethod) ||
            oauthSignatureMethod != OAuthHmacSignatureMethod)
            return false;

        // TODO: Check if nonce is not reused (is actually needed?)
        //if (!oauthValues.TryGetValue("oauth_nonce", out string? oauthNonce) ||
        //    NONCE IS NOT UNIQUE)
        //    return false;

        string actualBodyHash = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(body)));

        if (!oauthValues.TryGetValue("oauth_body_hash", out string? oauthBodyHash) ||
            oauthBodyHash != actualBodyHash)
            return false;

        if (!oauthValues.TryGetValue("oauth_consumer_key", out string? oauthConsumerKey) ||
            oauthConsumerKey != OAuthConsumerKey)
            return false;

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // TODO: Add option to ignore timestamp
        if (!oauthValues.TryGetValue("oauth_timestamp", out string? oauthTimestamp) ||
            !Int64.TryParse(oauthTimestamp, out long oauthTimestampLong) ||
            oauthTimestampLong > now.ToUnixTimeSeconds() ||
            oauthTimestampLong < (now - TimeSpan.FromSeconds(OAuthTimeoutSeconds)).ToUnixTimeSeconds())
            return false;

        if (!oauthValues.TryGetValue("oauth_signature", out string? oauthSignature))
            return false;

        string actualSignature =
            Convert.ToBase64String(HmacSigner.SignData(BuildStringToSign(httpMethod, url, oauthValues), SecretHmacKey));

        if (oauthSignature != actualSignature)
            return false;

        return true;
    }

    public static bool VerifyOAuthRsa(string httpMethod, string url, string body, Dictionary<string, string> oauthValues, string publicKey)
    {
        if (!oauthValues.TryGetValue("oauth_version", out string? oauthVersion) ||
            oauthVersion != OAuthVersion)
            return false;

        if (!oauthValues.TryGetValue("oauth_signature_method", out string? oauthSignatureMethod) ||
            oauthSignatureMethod != OAuthRsaSignatureMethod)
            return false;

        // TODO: Check if nonce is not reused (is actually needed?)
        //if (!oauthValues.TryGetValue("oauth_nonce", out string? oauthNonce) ||
        //    NONCE IS NOT UNIQUE)
        //    return false;

        string actualBodyHash = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(body)));

        if (!oauthValues.TryGetValue("oauth_body_hash", out string? oauthBodyHash) ||
            oauthBodyHash != actualBodyHash)
            return false;

        if (!oauthValues.TryGetValue("oauth_consumer_key", out string? oauthConsumerKey) ||
            oauthConsumerKey != OAuthConsumerKey)
            return false;

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // TODO: Add option to ignore timestamp
        if (!oauthValues.TryGetValue("oauth_timestamp", out string? oauthTimestamp) ||
            !Int64.TryParse(oauthTimestamp, out long oauthTimestampLong) ||
            oauthTimestampLong > now.ToUnixTimeSeconds() ||
            oauthTimestampLong < (now - TimeSpan.FromSeconds(OAuthTimeoutSeconds)).ToUnixTimeSeconds())
            return false;

        if (!oauthValues.TryGetValue("oauth_signature", out string? oauthSignature))
            return false;

        byte[] publicKeyBytes = Convert.FromBase64String(publicKey);
        byte[] oauthSignatureBytes = Convert.FromBase64String(oauthSignature);

        if (!RsaSigner.VerifySignature(BuildStringToSign(httpMethod, url, oauthValues), oauthSignatureBytes, publicKeyBytes))
            return false;

        // Assume xoauth_requestor_id is verified here, because public key depends on it's user id

        if (!oauthValues.TryGetValue("xoauth_as_hash", out string? xoauthAsHash))
            return false;

        byte[] xoauthAsHashBytes = Convert.FromBase64String(xoauthAsHash);

        if (!RsaSigner.VerifySignature($"{SecretHmacKey}{oauthTimestamp}", xoauthAsHashBytes, publicKeyBytes))
            return false;

        return true;
    }
}
