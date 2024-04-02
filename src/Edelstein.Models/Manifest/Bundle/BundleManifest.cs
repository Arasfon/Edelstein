using System.Runtime.Serialization;

namespace Edelstein.Models.Manifest.Bundle;

[Serializable]
public class BundleManifest : ISerializable
{
    public required BundleManifestEntry[] Entries { get; init; }

    public BundleManifest() { }

    public BundleManifest(SerializationInfo info, StreamingContext context) =>
        Entries = (BundleManifestEntry[])info.GetValue("m_manifestCollection", typeof(BundleManifestEntry[]))!;

    public void GetObjectData(SerializationInfo info, StreamingContext context) { }
}
