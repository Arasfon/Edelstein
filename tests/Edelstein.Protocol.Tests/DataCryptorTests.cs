namespace Edelstein.Protocol.Tests;

public class DataCryptorTests
{
    [Theory]
    [InlineData("""{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        """{"code":0,"server_time":1707577240,"data":{"asset_hash":"5f71eb3c8d3a700c4b89550dabf1ed2f"}}""")]
    public void AssetHashMessageLoop(string clientData, string serverData)
    {
        string encryptedClientData = DataCryptor.ClientEncrypt(clientData, out byte[] clientIv);

        string serverReceivedData = DataCryptor.ServerDecrypt(encryptedClientData, out byte[] serverReceivedIv);

        Assert.Equal(clientData, serverReceivedData);

        string encryptedServerData = DataCryptor.ServerEncrypt(serverData, serverReceivedIv);

        string clientReceivedData = DataCryptor.ClientDecrypt(encryptedServerData, clientIv);

        Assert.Equal(serverData, clientReceivedData);
    }
}
