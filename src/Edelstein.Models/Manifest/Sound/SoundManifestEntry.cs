using System.Runtime.Serialization;

namespace Edelstein.Models.Manifest.Sound;

[Serializable]
public class SoundManifestEntry : IManifestEntry, ISerializable
{
    public required string Identifier { get; init; }

    public required string Name { get; init; }

    public required string Hash { get; init; }

    public required string AcbHash { get; init; }

    public long AcbSize { get; init; }

    public required string AcbPrimaryPartHash { get; init; }

    public long AcbPrimaryPartSize { get; init; }

    public required string AcbSecondaryPartHash { get; init; }

    public long AcbSecondaryPartSize { get; init; }

    public required string AwbHash { get; init; }

    public long AwbSize { get; init; }

    public required string AwbPrimaryPartHash { get; init; }

    public long AwbPrimaryPartSize { get; init; }

    public required string AwbSecondaryPartHash { get; init; }

    public long AwbSecondaryPartSize { get; init; }

    public required string[] Labels { get; init; }

    public bool EnableSplit { get; init; }

    public SoundManifestEntry() { }

    public SoundManifestEntry(SerializationInfo info, StreamingContext context)
    {
        Identifier = info.GetString("m_identifier")!;
        Name = info.GetString("m_name")!;
        Hash = info.GetString("m_hash")!;
        AcbHash = info.GetString("m_acbHash")!;
        AcbSize = info.GetInt64("m_acbSize");
        AcbPrimaryPartHash = info.GetString("m_acbPrimaryPartHash")!;
        AcbPrimaryPartSize = info.GetInt64("m_acbPrimaryPartSize");
        AcbSecondaryPartHash = info.GetString("m_acbSecondaryPartHash")!;
        AcbSecondaryPartSize = info.GetInt64("m_acbSecondaryPartSize");
        AwbHash = info.GetString("m_awbHash")!;
        AwbSize = info.GetInt64("m_awbSize");
        AwbPrimaryPartHash = info.GetString("m_awbPrimaryPartHash")!;
        AwbPrimaryPartSize = info.GetInt64("m_awbPrimaryPartSize");
        AwbSecondaryPartHash = info.GetString("m_awbSecondaryPartHash")!;
        AwbSecondaryPartSize = info.GetInt64("m_awbSecondaryPartSize");
        Labels = (string[])info.GetValue("m_labels", typeof(string[]))!;
        EnableSplit = info.GetBoolean("m_enableSplit");
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) { }
}
