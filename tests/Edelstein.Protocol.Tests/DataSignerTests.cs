namespace Edelstein.Protocol.Tests;

public class DataSignerTests
{
    [Theory]
    [InlineData(0, "1.10.1", 1707614579, """{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        "ghQURYm3lmRz8NmVl7iam9ljUCZBe4Zb3HvuqCvkSOoh3xg0P5cT7DSIE96t2Hah6DexowAYdWSeQFq6S+N1uQ==")]
    public void VerifyRequestSignature(long userId, string clientVersion, long timestamp, string jsonData, string signature)
    {
        bool isVerified = DataSigner.VerifyRequestSignature(userId, clientVersion, timestamp, jsonData, signature);

        Assert.True(isVerified);
    }

    [Theory]
    [InlineData(0, "1.10.1", 1707614579, """{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        "ghQURYm3lmRz8NmVl7iam9ljUCZBe4Zb3HvuqCvkSOoh3xg0P5cT7DSIE96t2Hah6DexowAYdWSeQFq6S+N1uQ==")]
    public void RequestSignatureLoop(long userId, string clientVersion, long timestamp, string jsonData, string expectedSignature)
    {
        string signature = DataSigner.SignRequest(userId, clientVersion, timestamp, jsonData);

        Assert.Equal(expectedSignature, signature);

        bool isVerified = DataSigner.VerifyRequestSignature(userId, clientVersion, timestamp, jsonData, signature);

        Assert.True(isVerified);
    }
}
