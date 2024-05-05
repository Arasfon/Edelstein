namespace Edelstein.Server.Configuration.Assets;

public class AssetsOptions
{
    public string Path { get; set; } = "";
    public required AssetHashesOptions Hashes { get; set; }
}
