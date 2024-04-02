using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edelstein.Models.Serialization;

public class UlongOverflowingBsonSerializer : SerializerBase<ulong>
{
    public override ulong Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        long storedValue = context.Reader.ReadInt64();
        return (ulong)storedValue;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ulong value) =>
        context.Writer.WriteInt64((long)value);
}
