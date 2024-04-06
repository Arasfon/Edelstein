namespace Edelstein.Security.Tests;

public class RequestSignerTests
{
    [Theory]
    [InlineData(0, "1.10.1", 1707614579, """{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        "ghQURYm3lmRz8NmVl7iam9ljUCZBe4Zb3HvuqCvkSOoh3xg0P5cT7DSIE96t2Hah6DexowAYdWSeQFq6S+N1uQ==",
        "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMes5UJdSUibcJVG3Nrf53j0ObVFy6XHcCeFSij06d4mQlQ4CFEQ4UNQawM9dPINsYDIm9aipzdBTgNScbqZUJUCAwEAAQ==")]
    public void AuthorizeRequest(ulong xuid, string clientVersion, long timestamp, string jsonData, string signature,
        string publicKey)
    {
        bool isVerified = RequestSigner.AuthorizeRequest(xuid, clientVersion, timestamp, jsonData, signature,
            publicKey);

        Assert.True(isVerified);
    }

    [Theory]
    [InlineData(100000000000001, "1.11.2", 1712357389, """{"asset_version":"4c921d2443335e574a82e04ec9ea243c","environment":"release"}""",
        "VmnITQVWE3xl0OHgFQ7efdL7cjq17ndntc3+SeyRFCGyA55PA9ysmiR5tMqplf0kwAYbZAbeKNknhXoC1maUjA==",
        "MIIBVAIBADANBgkqhkiG9w0BAQEFAASCAT4wggE6AgEAAkEAwmqQ4T2s3bzw880lTvT+Lz/eP5nrIb9z132WDpvY7U/h6xoq425ji+qoApt0AptvCBwZQDFqY+7LUZQbO3mc5wIDAQABAkAMygQq1QBPUTO2jgWzb86yyVbzmcqhHIjgduyinGJE8LwGbF4+H7/Px+QauZwGbujAmZ42Wdhh40opHA8WjmipAiEA80v3ZItXUbK9+c66UC81wR4YT/3ZmTldxWQE1dQ/dxkCIQDMkTy5Y8JHJo4pIjDsPi/1YNLMYOQXDarmfeu8IMgz/wIgPpcgggFG6YGuVgHU9KPwlEoFvy5zOHQFp65fgBMmcKECIAwnnQoO6V485jl/dJS05TiZNi06nJLaFwt28+jLsdmVAiEAxPle8g3ChpDUpiaaUMacuwmDh6YA5GoRFLHqZ60bT6g=",
        "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMJqkOE9rN288PPNJU70/i8/3j+Z6yG/c9d9lg6b2O1P4esaKuNuY4vqqAKbdAKbbwgcGUAxamPuy1GUGzt5nOcCAwEAAQ==")]
    public void RequestSignatureLoop(ulong xuid, string clientVersion, long timestamp, string jsonData, string expectedSignature,
        string privateKey, string publicKey)
    {
        string signature = RequestSigner.SignRequest(xuid, clientVersion, timestamp, jsonData, privateKey);

        Assert.Equal(expectedSignature, signature);

        bool isVerified = RequestSigner.AuthorizeRequest(xuid, clientVersion, timestamp, jsonData, signature,
            publicKey);

        Assert.True(isVerified);
    }
}
