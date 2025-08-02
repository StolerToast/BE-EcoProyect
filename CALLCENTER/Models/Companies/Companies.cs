using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Npgsql;
using smartbin.DataAccess;
using smartbin.PostModels;
using System.Data;

namespace smartbin.Models.Companies
{
    public class Companies
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("company_id")]
        public string CompanyId { get; set; }

        [BsonElement("name")]
        public string Nombre { get; set; }

        [BsonElement("location")]
        public Location Coordenadas { get; set; }

        [BsonElement("contact")]
        public ContactInfo Contacto { get; set; }

        [BsonElement("active")]
        public bool Activa { get; set; } = true;

        [BsonElement("created_at")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [BsonElement("updated_at")]
        public DateTime? FechaActualizacion { get; set; } // Nullable por si no existe

        // Clases anidadas para la estructura MongoDB
        public class Location
        {
            [BsonElement("type")]
            public string Tipo { get; set; } = "Point";

            [BsonElement("coordinates")]
            public double[] Coordenadas { get; set; }
        }

        public class ContactInfo
        {
            [BsonElement("email")]
            public string Email { get; set; }

            [BsonElement("phone")]
            public string Telefono { get; set; }

            [BsonElement("address")]
            public string Direccion { get; set; }
        }

        // Método para obtener todas las empresas (híbrido)
        public static dynamic GetAll()
        {
            // 1. Consulta a PostgreSQL
            string query = @"
                SELECT c.mongo_company_id, c.name, c.email, c.phone, c.address, c.active
                FROM companies c
                WHERE c.active = true";

            var command = new NpgsqlCommand(query);
            DataTable pgResults = PostgreSqlConnection.ExecuteQuery(command);

            // 2. Consulta a MongoDB para obtener ubicaciones
            var mongoCollection = MongoDbConnection.GetCollection<Companies>("companies");
            var mongoResults = mongoCollection.Find(Builders<Companies>.Filter.Empty).ToList();

            // 3. Combinar resultados
            var combined = pgResults.AsEnumerable().Select(pgRow => new
            {
                CompanyId = pgRow.Field<string>("mongo_company_id"),
                Nombre = pgRow.Field<string>("name"),
                Email = pgRow.Field<string>("email"),
                Telefono = pgRow.Field<string>("phone"),
                Direccion = pgRow.Field<string>("address"),
                Activa = pgRow.Field<bool>("active"),
                Ubicacion = mongoResults.FirstOrDefault(m => m.CompanyId == pgRow.Field<string>("mongo_company_id"))?.Coordenadas
            }).ToList();

            return combined;
        }

        // Método para insertar (híbrido)
        public static dynamic Insert(PostCompanies nuevaCompanies)
        {
            // Generar ID único
            string companyId = $"COMP-{DateTime.UtcNow:yyyyMMddHHmmss}";

            // 1. Insertar en MongoDB
            var mongoEmpresa = new Companies
            {
                CompanyId = companyId,
                Nombre = nuevaCompanies.Nombre,
                Coordenadas = new Location
                {
                    Coordenadas = nuevaCompanies.Ubicacion
                        .Split(',')
                        .Select(double.Parse)
                        .ToArray()
                },
                FechaActualizacion = DateTime.UtcNow,
                Contacto = new ContactInfo
                {
                    Email = nuevaCompanies.Email,
                    Telefono = nuevaCompanies.Telefono,
                    Direccion = nuevaCompanies.Direccion
                }
            };

            var mongoCollection = MongoDbConnection.GetCollection<Companies>("companies");
            mongoCollection.InsertOne(mongoEmpresa);

            // 2. Insertar en PostgreSQL
            string pgQuery = @"
                INSERT INTO companies (mongo_company_id, name, email, phone, address)
                VALUES (@mongoId, @nombre, @email, @telefono, @direccion)";

            var pgCommand = new NpgsqlCommand(pgQuery);
            pgCommand.Parameters.AddWithValue("@mongoId", companyId);
            pgCommand.Parameters.AddWithValue("@nombre", nuevaCompanies.Nombre);
            pgCommand.Parameters.AddWithValue("@email", nuevaCompanies.Email);
            pgCommand.Parameters.AddWithValue("@telefono", nuevaCompanies.Telefono);
            pgCommand.Parameters.AddWithValue("@direccion", nuevaCompanies.Direccion);

            PostgreSqlConnection.ExecuteNonQuery(pgCommand);

            return new { companyId };
        }
    }
}