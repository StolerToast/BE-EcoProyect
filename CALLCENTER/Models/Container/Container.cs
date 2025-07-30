using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using smartbin.DataAccess;
using System.Collections.Generic;

namespace smartbin.Models.Container
{
    public class Container
    {
        #region attributes

        [BsonId]
        [BsonElement("_id")]
        public ObjectId IdMongo { get; set; }

        [BsonElement("cont_id")]
        public string ContId { get; set; } = "";

        [BsonElement("device_id")]
        public string DeviceId { get; set; } = "";

        [BsonElement("ubicacion")]
        public double[] Ubicacion { get; set; } = [];

        [BsonElement("estado")]
        public string Estado { get; set; } = "";

        #endregion

        #region constructors

        /// <summary>
        /// Creates an empty object
        /// </summary>
        public Container() { }

        /// <summary>
        /// Creates an object with data from the arguments
        /// </summary>
        /// <param name="cont_id">Container id</param>
        /// <param name="device_id">Container id</param>
        /// <param name="ubicacion">Location</param>
        /// <param name="estado">State</param>
        public Container(string cont_id, string device_id, double[] ubicacion, string estado)
        {
            ContId = cont_id;
            DeviceId = device_id;
            Ubicacion = ubicacion;
            Estado = estado;
        }

        #endregion

        #region class methods

        /// <summary>
        /// Returns a list of all the containers
        /// </summary>
        /// <returns></returns>
        public static List<Container> Get()
        {
            var collection = MongoDbConnection.GetCollection<Container>("container");
            return collection.Find(FilterDefinition<Container>.Empty).ToList();
        }

        /// <summary>
        /// Returns a container by its container id
        /// </summary>
        /// <param name="contId">Container id</param>
        /// <returns></returns>
        public static Container? GetByContId(string contId)
        {
            var collection = MongoDbConnection.GetCollection<Container>("container");
            var filter = Builders<Container>.Filter.Eq(c => c.ContId, contId);
            return collection.Find(filter).FirstOrDefault();
        }

        /// <summary>
        /// Inserts the current container instance into the database
        /// </summary>
        public void Insert()
        {
            var collection = MongoDbConnection.GetCollection<Container>("container");
            collection.InsertOne(this);
        }

        #endregion
    }
}
