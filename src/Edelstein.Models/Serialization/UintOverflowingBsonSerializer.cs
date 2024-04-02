using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edelstein.Models.Serialization;

public class UintOverflowingBsonSerializer : SerializerBase<uint>
{
    public override uint Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        int storedValue = context.Reader.ReadInt32();
        return (uint)storedValue;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, uint value) =>
        context.Writer.WriteInt32((int)value);
}
