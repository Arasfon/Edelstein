using System.Runtime.Serialization;

namespace Edelstein.Models.Manifest.Bundle;

public class BundleTypesSerializationBinder : SerializationBinder
{
    public override Type? BindToType(string assemblyName, string typeName) =>
        typeName switch
        {
            "ShockBinaryBundleSingleManifest" => typeof(BundleManifest),
            "ShockBinaryBundleManifest" => typeof(BundleManifestEntry),
            _ => null
        };
}
