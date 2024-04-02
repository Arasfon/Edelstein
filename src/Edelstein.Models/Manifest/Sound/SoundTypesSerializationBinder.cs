using System.Runtime.Serialization;

namespace Edelstein.Models.Manifest.Sound;

public class SoundTypesSerializationBinder : SerializationBinder
{
    public override Type? BindToType(string assemblyName, string typeName) =>
        typeName switch
        {
            "ShockBinarySoundSingleManifest" => typeof(SoundManifest),
            "ShockBinarySoundManifest" => typeof(SoundManifestEntry),
            _ => null
        };
}
