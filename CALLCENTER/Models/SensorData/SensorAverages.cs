// En Models/SensorData/SensorAverages.cs
using MongoDB.Bson.Serialization.Attributes;

namespace smartbin.Models.SensorData
{
    public class SensorAverages
    {
        [BsonElement("avg_temperature")]
        public double AvgTemperature { get; set; }

        [BsonElement("avg_humidity")]
        public double AvgHumidity { get; set; }

        [BsonElement("avg_methane")]
        public double AvgMethane { get; set; }

        [BsonElement("avg_co2")]
        public double AvgCO2 { get; set; }

        [BsonElement("avg_fill_level")]
        public double AvgFillLevel { get; set; }

        [BsonElement("devices_count")]
        public int DevicesCount { get; set; }
    }
}