namespace Edelstein.Security.Tests;

public class RequestSignerTests
{
    [Theory]
    [InlineData(0, "1.10.1", 1707614579, """{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        "ghQURYm3lmRz8NmVl7iam9ljUCZBe4Zb3HvuqCvkSOoh3xg0P5cT7DSIE96t2Hah6DexowAYdWSeQFq6S+N1uQ==",
        "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMes5UJdSUibcJVG3Nrf53j0ObVFy6XHcCeFSij06d4mQlQ4CFEQ4UNQawM9dPINsYDIm9aipzdBTgNScbqZUJUCAwEAAQ==")]
    public void AuthorizeRequest(long userId, string clientVersion, long timestamp, string jsonData, string signature,
        string publicKey)
    {
        bool isVerified = RequestSigner.AuthorizeRequest(userId, clientVersion, timestamp, jsonData, signature,
            publicKey);

        Assert.True(isVerified);
    }

    [Theory]
    [InlineData(0, "1.10.1", 1707614579, """{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        "ghQURYm3lmRz8NmVl7iam9ljUCZBe4Zb3HvuqCvkSOoh3xg0P5cT7DSIE96t2Hah6DexowAYdWSeQFq6S+N1uQ==",
        "MIIBVAIBADANBgkqhkiG9w0BAQEFAASCAT4wggE6AgEAAkEAx6zlQl1JSJtwlUbc2t/nePQ5tUXLpcdwJ4VKKPTp3iZCVDgIURDhQ1BrAz108g2xgMib1qKnN0FOA1JxuplQlQIDAQABAkAfy/ZRKGJOY+RcfSWX3C5z6i5TYcsxec4wGO9rkL66P4THAdw8qoEz6ITwyGtBaDcoXqvLliXDeXC5QZ9fDQV5AiEA8bN0RSpHS3FIMdsp37LJVEecWp+KUHZdzGZlaXderA0CIQDTfPdRcU2lJDhd61/mkWZisP/aAv0ms9u7SknbOkWsqQIgDdR85LBEoBZ9UQz/WmN6ja4DTlQur+f1+gop510DN8kCIEvqTsjgWBPQhZ9JD1qvlMYWbTDv47lR1K1NRGe6aipJAiEAlDRODHltMryJZlFrwONcGAIUzYxHFILBI/y767SUl6o=",
        "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAMes5UJdSUibcJVG3Nrf53j0ObVFy6XHcCeFSij06d4mQlQ4CFEQ4UNQawM9dPINsYDIm9aipzdBTgNScbqZUJUCAwEAAQ==")]
    public void RequestSignatureLoop(long userId, string clientVersion, long timestamp, string jsonData, string expectedSignature,
        string privateKey, string publicKey)
    {
        string signature = RequestSigner.SignRequest(userId, clientVersion, timestamp, jsonData, privateKey);

        Assert.Equal(expectedSignature, signature);

        bool isVerified = RequestSigner.AuthorizeRequest(userId, clientVersion, timestamp, jsonData, signature,
            publicKey);

        Assert.True(isVerified);
    }
}
