namespace Edelstein.Protocol.Tests;

public class ProtocolCryptorTests
{
    [Theory]
    [InlineData("""{"asset_version":"30c18961cf5e094e5d68c920d5f74d69","environment":"release"}""",
        """{"code":0,"server_time":1707577240,"data":{"asset_hash":"5f71eb3c8d3a700c4b89550dabf1ed2f"}}""")]
    public void AssetHashMessageLoop(string clientData, string serverData)
    {
        string encryptedClientData = ProtocolCryptor.Encrypt(clientData);

        string serverReceivedData = ProtocolCryptor.Decrypt(encryptedClientData);

        Assert.Equal(clientData, serverReceivedData);

        string encryptedServerData = ProtocolCryptor.Encrypt(serverData);

        string clientReceivedData = ProtocolCryptor.Decrypt(encryptedServerData);

        Assert.Equal(serverData, clientReceivedData);
    }
}
