namespace Edelstein.Security.Tests;

public class PayloadCryptorTests
{
    [Theory]
    [InlineData("""{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        """{"code":0,"server_time":1707577240,"data":{"asset_hash":"5f71eb3c8d3a700c4b89550dabf1ed2f"}}""")]
    public async Task AssetHashMessageLoop(string clientData, string serverData)
    {
        string encryptedClientData = await PayloadCryptor.EncryptAsync(clientData);

        string serverReceivedData = PayloadCryptor.Decrypt(encryptedClientData);

        Assert.Equal(clientData, serverReceivedData);

        string encryptedServerData = await PayloadCryptor.EncryptAsync(serverData);

        string clientReceivedData = PayloadCryptor.Decrypt(encryptedServerData);

        Assert.Equal(serverData, clientReceivedData);
    }
}
