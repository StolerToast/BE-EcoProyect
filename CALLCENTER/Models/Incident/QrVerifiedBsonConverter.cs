using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Attributes;

namespace smartbin.Models.Incident
{
    public class QrVerifiedBsonConverter : IBsonSerializer<bool>
    {
        public Type ValueType => typeof(bool);

        public bool Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Boolean:
                    return context.Reader.ReadBoolean();
                case BsonType.String:
                    var str = context.Reader.ReadString();
                    return str == "true" || str == "1";
                case BsonType.Int32:
                    return context.Reader.ReadInt32() != 0;
                case BsonType.Null:
                    context.Reader.ReadNull();
                    return false;
                default:
                    throw new BsonSerializationException($"Cannot deserialize QrVerified from BsonType {bsonType}");
            }
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, bool value)
        {
            context.Writer.WriteBoolean(value);
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => Deserialize(context, args);

        void IBsonSerializer.Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
            => Serialize(context, args, (bool)value);
    }
}