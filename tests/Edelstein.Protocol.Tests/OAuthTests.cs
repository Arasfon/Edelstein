namespace Edelstein.Protocol.Tests;

public class OAuthTests
{
    [Fact]
    public void BuildStringToSign()
    {
        const string httpMethod = "POST";
        const string url = "https://gl-payment.gree-apps.net/v1.0/auth/authorize";
        Dictionary<string, string> oauthValues = new()
        {
            ["oauth_body_hash"] = "2jmj7l5rSw0yVb/vlWAYkK/YBwk=",
            ["oauth_consumer_key"] = "232610769078541",
            ["oauth_nonce"] = "-1018017306293281741",
            ["oauth_signature_method"] = "RSA-SHA1",
            ["oauth_timestamp"] = "1708770124",
            ["oauth_version"] = "1.0",
            ["xoauth_as_hash"] = "Tm2cIOeqW5vPnvE92XhaPIAYpQPkPv8t7A4SoPnMAPt3Xho4U2s1dpt6gKmYrhxsBn1q/W6uVcH+RWqbZrGwjA==",
            ["xoauth_requestor_id"] = "2326107690785413ffb1b4049d2095f392408b7cfc9d94c"
        };

        string result = OAuth.BuildStringToSign(httpMethod, url, oauthValues);

        Assert.Equal(
            "POST&https%3A%2F%2Fgl-payment.gree-apps.net%2Fv1.0%2Fauth%2Fauthorize&oauth_body_hash%3D2jmj7l5rSw0yVb%252FvlWAYkK%252FYBwk%253D%26oauth_consumer_key%3D232610769078541%26oauth_nonce%3D-1018017306293281741%26oauth_signature_method%3DRSA-SHA1%26oauth_timestamp%3D1708770124%26oauth_version%3D1.0%26xoauth_as_hash%3DTm2cIOeqW5vPnvE92XhaPIAYpQPkPv8t7A4SoPnMAPt3Xho4U2s1dpt6gKmYrhxsBn1q%252FW6uVcH%252BRWqbZrGwjA%253D%253D%26xoauth_requestor_id%3D2326107690785413ffb1b4049d2095f392408b7cfc9d94c",
            result);
    }

    [Fact]
    public void HmacSignature()
    {
        const string httpMethod = "POST";
        const string url = "https://gl-payment.gree-apps.net/v1.0/auth/authorize";

        const string body = "";

        Dictionary<string, string> oauthValues = new()
        {
            ["oauth_version"] = "1.0",
            ["oauth_nonce"] = "933224d181da3515b76ea5372654e04a",
            ["oauth_timestamp"] = "1708779691",
            ["oauth_consumer_key"] = "232610769078541",
            ["oauth_body_hash"] = "2jmj7l5rSw0yVb/vlWAYkK/YBwk=",
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_signature"] = "rkpFfAae2J3yGt5v2NKwZxhDWn0="
        };

        Assert.True(OAuth.VerifyOAuthHmac(httpMethod, url, body, oauthValues));
    }
}
