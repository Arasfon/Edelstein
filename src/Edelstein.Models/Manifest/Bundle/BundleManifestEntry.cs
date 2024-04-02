using System.Runtime.Serialization;

namespace Edelstein.Models.Manifest.Bundle;

[Serializable]
public class BundleManifestEntry : IManifestEntry, ISerializable
{
    public required string Identifier { get; init; }

    public required string Name { get; init; }

    public required string Hash { get; init; }

    public uint Crc { get; init; }

    public long Length { get; init; }

    public required string[] Dependencies { get; init; }

    public required string[] Labels { get; init; }

    public required string[] Assets { get; init; }

    public BundleManifestEntry() { }

    public BundleManifestEntry(SerializationInfo info, StreamingContext context)
    {
        Identifier = info.GetString("m_identifier")!;
        Name = info.GetString("m_name")!;
        Hash = info.GetString("m_hash")!;
        Crc = info.GetUInt32("m_crc");
        Length = info.GetInt64("m_length");
        Dependencies = (string[])info.GetValue("m_dependencies", typeof(string[]))!;
        Labels = (string[])info.GetValue("m_labels", typeof(string[]))!;
        Assets = (string[])info.GetValue("m_assets", typeof(string[]))!;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) { }
}
