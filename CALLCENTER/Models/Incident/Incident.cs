using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
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
            var pipeline = new[]
            {
        // Filtro inicial (solo complaints/damage)
        new BsonDocument("$match", new BsonDocument("type", new BsonDocument("$in", new BsonArray { "complaint", "damage" }))),

        // Lookup para datos del empleado (SQL_users)
        new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "SQL_users" },
            { "localField", "reported_by" },
            { "foreignField", "user_id" },
            { "as", "empleado" }
        }),
        new BsonDocument("$unwind", "$empleado"),

        // Lookup para datos de la compañía (companies)
        new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "companies" },
            { "localField", "company_id" },
            { "foreignField", "company_id" },
            { "as", "company_info" }
        }),
        new BsonDocument("$unwind", "$company_info"),

        // Proyección final (campos a devolver)
        new BsonDocument("$project", new BsonDocument
        {
            { "_id", 0 },
            { "incident_id", 1 },               // Nuevo campo
            { "type", 1 },                     // Nuevo campo
            { "container_id", 1 },
            { "descripcion", "$description" },
            { "fecha", "$created_at" },
            { "imagen", new BsonDocument("$arrayElemAt", new BsonArray { "$images.url", 0 }) },
            { "nombre_empleado", new BsonDocument("$concat", new BsonArray { "$empleado.nombre", " ", "$empleado.apellido" }) },
            { "company_name", "$company_info.name" }  // Nombre en lugar del ID
        })
    };
            return collection.Aggregate<BsonDocument>(pipeline).ToList();
        }

        // Consulta incidentes resueltos
        public static List<BsonDocument> GetSolvedIncidents(IMongoDatabase db)
        {
            var collection = db.GetCollection<BsonDocument>("incidents");
            var pipeline = new[]
            {
        // Filtro inicial (solo complaints/damage)
        new BsonDocument("$match", new BsonDocument("type", new BsonDocument("$in", new BsonArray { "complaint" }))),

        // Lookup para datos del empleado (SQL_users)
        new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "SQL_users" },
            { "localField", "reported_by" },
            { "foreignField", "user_id" },
            { "as", "empleado" }
        }),
        new BsonDocument("$unwind", "$empleado"),

        // Lookup para datos de la compañía (companies)
        new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "companies" },
            { "localField", "company_id" },
            { "foreignField", "company_id" },
            { "as", "company_info" }
        }),
        new BsonDocument("$unwind", "$company_info"),

        // Proyección final (campos a devolver)
        new BsonDocument("$project", new BsonDocument
        {
            { "_id", 0 },
            { "incident_id", 1 },               // Nuevo campo
            { "type", 1 },                     // Nuevo campo
            { "container_id", 1 },
            { "descripcion", "$description" },
            { "fecha", "$created_at" },
            { "imagen", new BsonDocument("$arrayElemAt", new BsonArray { "$images.url", 0 }) },
            { "nombre_empleado", new BsonDocument("$concat", new BsonArray { "$empleado.nombre", " ", "$empleado.apellido" }) },
            { "company_name", "$company_info.name" }  // Nombre en lugar del ID
        })
    };
            return collection.Aggregate<BsonDocument>(pipeline).ToList();
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
