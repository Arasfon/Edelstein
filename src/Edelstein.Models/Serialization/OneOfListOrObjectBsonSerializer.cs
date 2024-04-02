using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

using OneOf;

namespace Edelstein.Models.Serialization;

public class OneOfListOrObjectBsonSerializer<T> : SerializerBase<OneOf<List<T>, T>>
{
    public override OneOf<List<T>, T> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        IBsonReader? bsonReader = context.Reader;

        if (bsonReader.CurrentBsonType == BsonType.Array)
        {
            List<T>? list = BsonSerializer.Deserialize<List<T>>(bsonReader);
            return list;
        }

        T? item = BsonSerializer.Deserialize<T>(bsonReader);
        return item;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, OneOf<List<T>, T> value) =>
        value.Switch(list => BsonSerializer.Serialize(context.Writer, list),
            item => BsonSerializer.Serialize(context.Writer, item));
}
