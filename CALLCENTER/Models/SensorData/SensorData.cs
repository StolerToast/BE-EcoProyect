using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using smartbin.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace smartbin.Models.SensorData
{
    public partial class SensorData
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("device_id")]
        public string DeviceId { get; set; } = "";

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("temperatura")]
        public double Temperatura { get; set; }

        [BsonElement("humedad")]
        public double Humedad { get; set; }

        [BsonElement("metano")]
        public double Metano { get; set; }

        [BsonElement("co2")]
        public double CO2 { get; set; }

        [BsonElement("nivel_llenado")]
        public double NivelLlenado { get; set; }

        [BsonElement("ubicacion")]
        public Ubicacion Ubicacion { get; set; } = new Ubicacion();

        public static List<SensorData> GetAll()
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            return collection.Find(FilterDefinition<SensorData>.Empty).ToList();
        }

        public static List<SensorData> GetByDeviceId(string deviceId)
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            var filter = Builders<SensorData>.Filter.Eq(s => s.DeviceId, deviceId);
            return collection.Find(filter).SortBy(s => s.Timestamp).ToList();
        }

        public void Insert()
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            collection.InsertOne(this);
        }
    }

    public class Ubicacion
    {
        [BsonElement("type")]
        public string Type { get; set; } = "Point";

        [BsonElement("coordinates")]
        public double[] Coordinates { get; set; } = new double[0];
    }
}
