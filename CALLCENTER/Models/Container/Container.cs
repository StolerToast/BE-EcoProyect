using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using smartbin.DataAccess;
using System;
using System.Collections.Generic;

namespace smartbin.Models.Container
{
    public class Container
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("container_id")]
        public string ContainerId { get; set; } = "";

        [BsonElement("company_id")]
        public string CompanyId { get; set; } = "";

        [BsonElement("qr_code")]
        public string QrCode { get; set; } = "";

        [BsonElement("location")]
        public Location GeoLocation { get; set; } = new Location();

        [BsonElement("type")]
        public string Type { get; set; } = "normal"; // "normal" | "biohazard"

        [BsonElement("capacity")]
        public int Capacity { get; set; } // Litros

        [BsonElement("status")]
        public string Status { get; set; } = "active"; // "active" | "inactive" | "maintenance"

        [BsonElement("device_id")]
        public string DeviceId { get; set; } = "";

        [BsonElement("last_collection")]
        public DateTime LastCollection { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Métodos CRUD
        public static List<Container> GetAll()
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            return collection.Find(FilterDefinition<Container>.Empty).ToList();
        }

        public static Container GetById(string containerId)
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            var filter = Builders<Container>.Filter.Eq(c => c.ContainerId, containerId);
            return collection.Find(filter).FirstOrDefault();
        }

        public void Insert()
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            collection.InsertOne(this);
        }

        public static bool Update(string containerId, Container updatedContainer)
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            var filter = Builders<Container>.Filter.Eq(c => c.ContainerId, containerId);
            var result = collection.ReplaceOne(filter, updatedContainer);
            return result.ModifiedCount > 0;
        }

        public static bool Delete(string containerId)
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            var filter = Builders<Container>.Filter.Eq(c => c.ContainerId, containerId);
            var result = collection.DeleteOne(filter);
            return result.DeletedCount > 0;
        }

        public static long CountActiveContainers()
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            var filter = Builders<Container>.Filter.Eq(c => c.Status, "active");
            return collection.CountDocuments(filter);
        }
    }

    public class Location
    {
        [BsonElement("type")]
        public string Type { get; set; } = "Point";

        [BsonElement("coordinates")]
        public double[] Coordinates { get; set; } = Array.Empty<double>();
    }
}