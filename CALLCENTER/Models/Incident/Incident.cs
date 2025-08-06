using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Npgsql;
using smartbin.DataAccess;
using System;
using System.Collections.Generic;

namespace smartbin.Models.Incident
{
    public class Incident
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId IdMongo { get; set; }

        [BsonElement("incident_id")]
        public string IncidentId { get; set; } = "";

        [BsonElement("container_id")]
        public string ContainerId { get; set; } = "";

        [BsonElement("company_id")]
        public string CompanyId { get; set; } = "";

        [BsonElement("reported_by")]
        public int ReportedBy { get; set; }

        //[BsonElement("qr_verified")]
        //public bool QrVerified { get; set; }

        [BsonElement("qr_scan_id")]
        public string QrScanId { get; set; } = "";

        [BsonElement("title")]
        public string Title { get; set; } = "";

        [BsonElement("description")]
        public string Description { get; set; } = "";

        [BsonElement("type")]
        public string Type { get; set; } = "";

        [BsonElement("priority")]
        public string Priority { get; set; } = "";

        [BsonElement("status")]
        public string Status { get; set; } = "";

        [BsonElement("images")]
        public List<ImageInfo> Images { get; set; } = new();

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [BsonElement("resolution_notes")]
        public string? ResolutionNotes { get; set; }

        public class ImageInfo
        {
            [BsonElement("url")]
            public string Url { get; set; } = "";

            [BsonElement("uploaded_at")]
            public DateTime UploadedAt { get; set; }
        }

        // Consulta general
        public static List<Incident> GetGeneral()
        {
            var collection = MongoDbConnection.GetCollection<Incident>("incidents");
            var filter = Builders<Incident>.Filter.In(x => x.Type, new[] { "complaint", "damage" });
            return collection.Find(filter).ToList();
        }

        // Método actualizado en Incident.cs (dentro de la clase Incident)
        public static List<BsonDocument> GetSpecificIncidents(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>("incidents");

            // 1. Primero obtenemos los incidentes de MongoDB
            var incidents = collection.Find(new BsonDocument("type", new BsonDocument("$in", new BsonArray { "complaint", "damage" }))).ToList();

            var result = new List<BsonDocument>();

            using (var pgConnection = PostgreSqlConnection.GetConnection())
            {
                foreach (var incident in incidents)
                {
                    // 2. Para cada incidente, obtenemos el usuario de PostgreSQL
                    var reportedBy = incident["reported_by"].AsInt32;
                    var pgCommand = new NpgsqlCommand(
                        "SELECT nombre, apellido FROM users WHERE user_id = @userId",
                        pgConnection);
                    pgCommand.Parameters.AddWithValue("@userId", reportedBy);

                    using (var reader = pgCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var nombre = reader.GetString(0);
                            var apellido = reader.GetString(1);

                            // 3. Construimos el documento combinado
                            var doc = new BsonDocument
                    {
                        { "incident_id", incident["incident_id"] },
                        { "type", incident["type"] },
                        { "container_id", incident["container_id"] },
                        { "description", incident["description"] },
                        { "created_at", incident["created_at"] },
                        { "nombre_empleado", $"{nombre} {apellido}" },
                        // ... otros campos necesarios
                    };
                            result.Add(doc);
                        }
                    }
                }
            }

            return result;
        }

        public static List<BsonDocument> GetSolvedIncidents(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>("incidents");

            // 1. Obtener todos los incidentes resueltos
            var incidents = collection.Find(new BsonDocument("status", "resolved")).ToList();

            // 2. Obtener todos los user_ids únicos
            var userIds = incidents.Select(i => i["reported_by"].AsInt32).Distinct().ToList();

            var userNames = new Dictionary<int, (string, string)>();

            using (var pgConnection = PostgreSqlConnection.GetConnection())
            {
                // 3. Consulta batch a PostgreSQL
                var pgCommand = new NpgsqlCommand(
                    $"SELECT user_id, nombre, apellido FROM users WHERE user_id IN ({string.Join(",", userIds)})",
                    pgConnection);

                using (var reader = pgCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userNames[reader.GetInt32(0)] = (reader.GetString(1), reader.GetString(2));
                    }
                }
            }

            // 4. Construir respuesta combinada
            return incidents.Select(incident =>
            {
                var userId = incident["reported_by"].AsInt32;
                var (nombre, apellido) = userNames.ContainsKey(userId) ? userNames[userId] : ("Desconocido", "");

                return new BsonDocument
        {
            { "incident_id", incident["incident_id"] },
            { "type", incident["type"] },
            { "container_id", incident["container_id"] },
            { "description", incident["description"] },
            { "created_at", incident["created_at"] },
            { "nombre_empleado", $"{nombre} {apellido}" },
            // ... otros campos
        };
            }).ToList();
        }


        // Consulta GraphicIncident (agrupación por mes/año)
        public static List<BsonDocument> GetGraphicIncident(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>("incidents");
            var sixMonthsAgo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-6);
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "type", new BsonDocument("$in", new BsonArray { "complaint", "damage" }) },
                    { "created_at", new BsonDocument("$gte", sixMonthsAgo) }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "year", new BsonDocument("$year", "$created_at") },
                            { "month", new BsonDocument("$month", "$created_at") }
                        }
                    },
                    { "total", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$sort", new BsonDocument
                {
                    { "_id.year", 1 },
                    { "_id.month", 1 }
                })
            };
            return collection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        // Consulta RecentReport
        public static List<BsonDocument> GetRecentReport(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>("incidents");
            var filter = new BsonDocument
            {
                { "type", new BsonDocument("$in", new BsonArray { "complaint", "damage" }) },
                { "status", new BsonDocument("$nin", new BsonArray { "completed", "revisado", "resolved" }) }
            };
            var projection = new BsonDocument
            {
                { "_id", 0 },
                { "container_id", 1 },
                { "created_at", 1 }
            };
            var sort = Builders<BsonDocument>.Sort.Descending("created_at");
            return collection.Find(filter).Project(projection).Sort(sort).ToList();
        }

        // Actualizar incidente
        // Dentro de la clase Incident (Incident.cs)
        public static bool ResolveIncident(string incidentId, string resolutionNotes = null)
        {
            var collection = MongoDbConnection.GetCollection<Incident>("incidents");
            var filter = Builders<Incident>.Filter.Eq(x => x.IncidentId, incidentId);
            var update = Builders<Incident>.Update
                .Set(x => x.Status, "resolved")
                .Set(x => x.ResolvedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(resolutionNotes))
            {
                update = update.Set(x => x.ResolutionNotes, resolutionNotes);
            }

            var result = collection.UpdateOne(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
