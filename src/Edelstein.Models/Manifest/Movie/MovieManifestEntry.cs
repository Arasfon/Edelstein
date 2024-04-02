using System.Runtime.Serialization;

namespace Edelstein.Models.Manifest.Movie;

[Serializable]
public class MovieManifestEntry : IManifestEntry, ISerializable
{
    public required string Identifier { get; init; }

    public required string Name { get; init; }

    public required string Hash { get; init; }

    public required string UsmHash { get; init; }

    public long UsmSize { get; init; }

    public required string UsmPrimaryPartHash { get; init; }

    public long UsmPrimaryPartSize { get; init; }

    public required string UsmSecondaryPartHash { get; init; }

    public long UsmSecondaryPartSize { get; init; }

    public bool EnableSplit { get; init; }

    public required string[] Labels { get; init; }

    public MovieManifestEntry() { }

    public MovieManifestEntry(SerializationInfo info, StreamingContext context)
    {
        Identifier = info.GetString("m_identifier")!;
        Name = info.GetString("m_name")!;
        Hash = info.GetString("m_hash")!;
        UsmHash = info.GetString("m_usmHash")!;
        UsmSize = info.GetInt64("m_usmSize");
        UsmPrimaryPartHash = info.GetString("m_usmPrimaryPartHash")!;
        UsmPrimaryPartSize = info.GetInt64("m_usmPrimaryPartSize");
        UsmSecondaryPartHash = info.GetString("m_usmSecondaryPartHash")!;
        UsmSecondaryPartSize = info.GetInt64("m_usmSecondaryPartSize");
        EnableSplit = info.GetBoolean("m_enableSplit");
        Labels = (string[])info.GetValue("m_labels", typeof(string[]))!;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) { }
}
