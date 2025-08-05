using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Npgsql;
using smartbin.DataAccess;
using smartbin.PostModels;
using System;
using System.Data;
using System.Linq;

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
        public DateTime? FechaActualizacion { get; set; }

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

        private static string GenerateSequentialId()
        {
            var lastCompany = MongoDbConnection.GetCollection<Companies>("companies")
                .Find(Builders<Companies>.Filter.Empty)
                .SortByDescending(c => c.CompanyId)
                .FirstOrDefault();

            if (lastCompany == null)
                return "COMP-001";

            // Manejo seguro para extraer el número
            var parts = lastCompany.CompanyId.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int lastNumber))
            {
                // Si el formato no es correcto, empezamos de nuevo
                return "COMP-001";
            }

            return $"COMP-{(lastNumber + 1).ToString("D3")}";
        }

        public static dynamic GetAll()
        {
            string query = @"
                SELECT c.mongo_company_id, c.name, c.email, c.phone, c.address, c.active
                FROM companies c
                WHERE c.active = true";

            var command = new NpgsqlCommand(query);
            DataTable pgResults = PostgreSqlConnection.ExecuteQuery(command);

            var mongoCollection = MongoDbConnection.GetCollection<Companies>("companies");
            var mongoResults = mongoCollection.Find(Builders<Companies>.Filter.Empty).ToList();

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

        public static dynamic GetById(string companyId)
        {
            // 1. Consulta a PostgreSQL
            string pgQuery = @"
                SELECT c.mongo_company_id, c.name, c.email, c.phone, c.address, c.active
                FROM companies c
                WHERE c.mongo_company_id = @companyId AND c.active = true";

            var pgCommand = new NpgsqlCommand(pgQuery);
            pgCommand.Parameters.AddWithValue("@companyId", companyId);
            DataTable pgResult = PostgreSqlConnection.ExecuteQuery(pgCommand);

            if (pgResult.Rows.Count == 0)
                return null;

            // 2. Consulta a MongoDB
            var mongoCollection = MongoDbConnection.GetCollection<Companies>("companies");
            var mongoData = mongoCollection.Find(c => c.CompanyId == companyId).FirstOrDefault();

            // 3. Combinar resultados
            var pgRow = pgResult.Rows[0];
            return new
            {
                CompanyId = pgRow.Field<string>("mongo_company_id"),
                Nombre = pgRow.Field<string>("name"),
                Email = pgRow.Field<string>("email"),
                Telefono = pgRow.Field<string>("phone"),
                Direccion = pgRow.Field<string>("address"),
                Activa = pgRow.Field<bool>("active"),
                Ubicacion = mongoData?.Coordenadas
            };
        }

        public static dynamic InsertWithTransaction(PostCompanies nuevaCompany)
        {
            using (var pgConnection = PostgreSqlConnection.GetConnection())
            using (var pgTransaction = pgConnection.BeginTransaction())
            {
                try
                {
                    string companyId = GenerateSequentialId();

                    // 1. Insertar en PostgreSQL
                    string pgQuery = @"
                INSERT INTO companies 
                    (mongo_company_id, name, email, phone, address, active, created_at)
                VALUES 
                    (@mongoId, @nombre, @email, @telefono, @direccion, true, CURRENT_TIMESTAMP)
                RETURNING company_id";

                    var pgCommand = new NpgsqlCommand(pgQuery, pgConnection, pgTransaction);
                    pgCommand.Parameters.AddWithValue("@mongoId", companyId);
                    pgCommand.Parameters.AddWithValue("@nombre", nuevaCompany.Nombre);
                    pgCommand.Parameters.AddWithValue("@email", nuevaCompany.Email);
                    pgCommand.Parameters.AddWithValue("@telefono", nuevaCompany.Telefono ?? (object)DBNull.Value);
                    pgCommand.Parameters.AddWithValue("@direccion", nuevaCompany.Direccion ?? (object)DBNull.Value);

                    // Usar Convert.ToInt64 para evitar el error de Int32
                    long pgCompanyId = Convert.ToInt64(pgCommand.ExecuteScalar());

                    // 2. Insertar en MongoDB
                    var mongoCompany = new Companies
                    {
                        CompanyId = companyId,
                        Nombre = nuevaCompany.Nombre,
                        Coordenadas = new Location
                        {
                            Coordenadas = nuevaCompany.Ubicacion
                                .Split(',')
                                .Select(double.Parse)
                                .ToArray()
                        },
                        Contacto = new ContactInfo
                        {
                            Email = nuevaCompany.Email,
                            Telefono = nuevaCompany.Telefono,
                            Direccion = nuevaCompany.Direccion
                        },
                        Activa = true,
                        FechaCreacion = DateTime.UtcNow
                    };

                    MongoDbConnection.GetCollection<Companies>("companies").InsertOne(mongoCompany);

                    pgTransaction.Commit();
                    return new { companyId, pgCompanyId };
                }
                catch (Exception ex)
                {
                    pgTransaction.Rollback();
                    throw new Exception($"Error en transacción híbrida: {ex.Message}");
                }
            }
        }

        public static bool UpdateWithTransaction(string companyId, PostCompanies updatedCompany)
        {
            using (var pgConnection = PostgreSqlConnection.GetConnection())
            using (var pgTransaction = pgConnection.BeginTransaction())
            {
                try
                {
                    // 1. Actualizar PostgreSQL (sin updated_at)
                    var pgCommand = new NpgsqlCommand(@"
                UPDATE companies 
                SET name = @nombre, 
                    email = @email, 
                    phone = @telefono, 
                    address = @direccion
                WHERE mongo_company_id = @companyId",
                        pgConnection, pgTransaction);

                    pgCommand.Parameters.AddWithValue("@nombre", updatedCompany.Nombre);
                    pgCommand.Parameters.AddWithValue("@email", updatedCompany.Email);
                    pgCommand.Parameters.AddWithValue("@telefono", updatedCompany.Telefono);
                    pgCommand.Parameters.AddWithValue("@direccion", updatedCompany.Direccion);
                    pgCommand.Parameters.AddWithValue("@companyId", companyId);

                    int pgRows = pgCommand.ExecuteNonQuery();
                    if (pgRows == 0)
                    {
                        pgTransaction.Rollback();
                        return false;
                    }

                    // 2. Actualizar MongoDB (mantenemos updated_at aquí)
                    var mongoUpdate = Builders<Companies>.Update
                        .Set(c => c.Nombre, updatedCompany.Nombre)
                        .Set(c => c.Contacto.Email, updatedCompany.Email)
                        .Set(c => c.Contacto.Telefono, updatedCompany.Telefono)
                        .Set(c => c.Contacto.Direccion, updatedCompany.Direccion)
                        .Set(c => c.Coordenadas.Coordenadas, updatedCompany.Ubicacion.Split(',').Select(double.Parse).ToArray())
                        .Set(c => c.FechaActualizacion, DateTime.UtcNow);

                    var mongoResult = MongoDbConnection.GetCollection<Companies>("companies")
                        .UpdateOne(c => c.CompanyId == companyId, mongoUpdate);

                    pgTransaction.Commit();
                    return mongoResult.ModifiedCount > 0;
                }
                catch (Exception ex)
                {
                    pgTransaction.Rollback();
                    throw new Exception($"Error al actualizar: {ex.Message}");
                }
            }
        }

        public static bool DeactivateWithTransaction(string companyId)
        {
            using (var pgConnection = PostgreSqlConnection.GetConnection())
            using (var pgTransaction = pgConnection.BeginTransaction())
            {
                try
                {
                    // 1. Desactivar en PostgreSQL (sin updated_at)
                    var pgCommand = new NpgsqlCommand(@"
                UPDATE companies 
                SET active = false
                WHERE mongo_company_id = @companyId",
                        pgConnection, pgTransaction);

                    pgCommand.Parameters.AddWithValue("@companyId", companyId);
                    int pgRows = pgCommand.ExecuteNonQuery();

                    // 2. Desactivar en MongoDB
                    var mongoUpdate = Builders<Companies>.Update
                        .Set(c => c.Activa, false)
                        .Set(c => c.FechaActualizacion, DateTime.UtcNow);

                    var mongoResult = MongoDbConnection.GetCollection<Companies>("companies")
                        .UpdateOne(c => c.CompanyId == companyId, mongoUpdate);

                    pgTransaction.Commit();
                    return pgRows > 0 && mongoResult.ModifiedCount > 0;
                }
                catch (Exception ex)
                {
                    pgTransaction.Rollback();
                    throw new Exception($"Error al desactivar: {ex.Message}");
                }
            }
        }

        public static dynamic GetMapData()
        {
            // 1. Consulta a PostgreSQL para datos básicos
            string pgQuery = @"
        SELECT c.mongo_company_id, c.name, c.address
        FROM companies c
        WHERE c.active = true";

            var command = new NpgsqlCommand(pgQuery);
            DataTable pgResults = PostgreSqlConnection.ExecuteQuery(command);

            // 2. Consulta a MongoDB para coordenadas
            var mongoCollection = MongoDbConnection.GetCollection<Companies>("companies");
            var mongoResults = mongoCollection.Find(Builders<Companies>.Filter.Empty).ToList();

            // 3. Combinar resultados
            var mapData = pgResults.AsEnumerable().Select(pgRow =>
            {
                var companyId = pgRow.Field<string>("mongo_company_id");
                var mongoData = mongoResults.FirstOrDefault(m => m.CompanyId == companyId);

                return new
                {
                    Id = companyId,
                    Nombre = pgRow.Field<string>("name"),
                    Direccion = pgRow.Field<string>("address"),
                    Coordenadas = mongoData?.Coordenadas?.Coordenadas,
                    Contenedores = 5 // Valor fijo temporal
                };
            }).ToList();

            return mapData;
        }
    }
}