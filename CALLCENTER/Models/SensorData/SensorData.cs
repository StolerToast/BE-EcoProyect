using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using smartbin.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace smartbin.Models.SensorData
{
    public class SensorData
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("device_id")]
        public string DeviceId { get; set; } = "";

        [BsonElement("container_id")]
        public string ContainerId { get; set; } = ""; // Nuevo campo

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("readings")]
        public Readings SensorReadings { get; set; } = new Readings();

        [BsonElement("location")]
        public Location PointLocation { get; set; } = new Location();

        [BsonElement("alerts")]
        public List<string> Alerts { get; set; } = new List<string>();

        // Métodos existentes (actualizados para nueva estructura)
        public static List<SensorData> GetAll()
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            return collection.Find(FilterDefinition<SensorData>.Empty).ToList();
        }

        public static List<SensorData> GetByDeviceId(string deviceId)
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            var filter = Builders<SensorData>.Filter.Eq(s => s.DeviceId, deviceId);
            return collection.Find(filter).SortByDescending(s => s.Timestamp).ToList();
        }

        public void Insert()
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            collection.InsertOne(this);
        }
    }

    public class Readings
    {
        [BsonElement("temperature")]
        public double Temperature { get; set; }

        [BsonElement("humidity")]
        public double Humidity { get; set; }

        [BsonElement("methane")]
        public double Methane { get; set; }

        [BsonElement("co2")]
        public double CO2 { get; set; }

        [BsonElement("fill_level")]
        public double FillLevel { get; set; }

        [BsonElement("battery_level")]
        public double BatteryLevel { get; set; }
    }

    public class Location
    {
        [BsonElement("type")]
        public string Type { get; set; } = "Point";

        [BsonElement("coordinates")]
        public double[] Coordinates { get; set; } = Array.Empty<double>();
    }
}