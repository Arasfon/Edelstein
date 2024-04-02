using System.Runtime.Serialization;

namespace Edelstein.Models.Mst;

public class MstSerializationBinder : SerializationBinder
{
    public override Type? BindToType(string assemblyName, string typeName)
    {
        if (assemblyName is "123")
        {
            return typeof(int);
        }

        return typeof(String);
    }
}
