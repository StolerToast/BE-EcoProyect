using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using smartbin.DataAccess;
using smartbin.Models.User;
using System.Data;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPut("update/{userId}")]
        public ActionResult UpdateUser(
    int userId,
    [FromBody] UserUpdateRequest request,
    [FromServices] IAuthorizationService authService) // Opcional: para validar permisos
        {
            // Opcional: Validar que el usuario autenticado es el mismo que se quiere modificar
            // var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            // if (userId != currentUserId) return Forbid();

            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
                bool success = smartbin.Models.User.User.UpdateUserWithSync(
                    userId,
                    request.Username,
                    request.Nombre,
                    request.Apellido,
                    request.Email,
                    hashedPassword
                );

                return success
                    ? Ok(new { status = 0, message = "Usuario actualizado" })
                    : NotFound(new { status = 1, message = "Usuario no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 2, message = ex.Message });
            }
        }

        [HttpGet("admin/validate")]
        public ActionResult ValidateAdminData()
        {
            try
            {
                // --- 1. Obtener datos de PostgreSQL (users) ---
                var pgCommand = new NpgsqlCommand(
                    @"SELECT username, contrasena_hash 
              FROM users 
              WHERE user_id = 1",  // ID fijo del admin
                    PostgreSqlConnection.GetConnection()
                );

                using (var pgReader = pgCommand.ExecuteReader())
                {
                    if (!pgReader.Read())
                    {
                        return NotFound(new { status = 1, message = "Usuario admin no encontrado en PostgreSQL" });
                    }

                    string pgUsername = pgReader.GetString(0);
                    string pgPasswordHash = pgReader.GetString(1);

                    // --- 2. Obtener datos de MongoDB (user_sync) ---
                    var mongoCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                    var mongoAdmin = mongoCollection.Find(Builders<BsonDocument>.Filter.Eq("sql_user_id", 1)).FirstOrDefault();

                    if (mongoAdmin == null)
                    {
                        return NotFound(new { status = 1, message = "Usuario admin no encontrado en MongoDB" });
                    }

                    // --- 3. Validar sincronización básica ---
                    bool isSynced = mongoAdmin["email"].AsString == pgUsername;  // Ejemplo: validar email = username (ajusta según tu lógica)

                    return Ok(new
                    {
                        status = 0,
                        postgres_data = new
                        {
                            username = pgUsername,
                            contrasena_hash = pgPasswordHash
                        },
                        is_synced = isSynced,
                        last_sync = mongoAdmin["last_sync"].ToUniversalTime()
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 2, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("{userId}")]
        public ActionResult GetUserCombined(int userId)
        {
            try
            {
                // 1. Obtener datos de PostgreSQL
                var pgCommand = new NpgsqlCommand(
                    @"SELECT user_id, username, nombre, apellido, email, role 
              FROM users 
              WHERE user_id = @userId",
                    PostgreSqlConnection.GetConnection()
                );
                pgCommand.Parameters.AddWithValue("@userId", userId);

                using (var reader = pgCommand.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return NotFound(new { status = 1, message = "Usuario no encontrado en PostgreSQL" });
                    }

                    // 2. Construir respuesta combinada (sin MongoDB)
                    var response = new UserCombinedResponse
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Nombre = reader.GetString(2),
                        Apellido = reader.GetString(3),
                        Email = reader.GetString(4),
                        Role = reader.GetString(5)
                    };

                    return Ok(new { status = 0, data = response });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 2, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost("CreateEmployeeUser")]
        public ActionResult CreateEmployeeUser([FromBody] CreateEmployeeRequest request)
        {
            using (var pgConnection = PostgreSqlConnection.GetConnection())
            using (var pgTransaction = pgConnection.BeginTransaction())
            {
                try
                {
                    // --- 1. Validar que la empresa exista en PostgreSQL ---
                    var companyId = GetCompanyIdByMongoId(request.CompanyMongoId);
                    if (companyId == null)
                    {
                        return BadRequest(new { status = 1, message = "Empresa no encontrada" });
                    }

                    // --- 2. Insertar en PostgreSQL (users) ---
                    var pgCommand = new NpgsqlCommand(
                        @"INSERT INTO users 
                  (username, nombre, apellido, email, contrasena_hash, role, telefono, company_id, active, created_at)
                  VALUES 
                  (@username, @nombre, @apellido, @email, @contrasenaHash, 'employee', @telefono, @companyId, true, CURRENT_TIMESTAMP)
                  RETURNING user_id;",
                        pgConnection,
                        pgTransaction
                    );

                    pgCommand.Parameters.AddWithValue("@username", request.Username);
                    pgCommand.Parameters.AddWithValue("@nombre", request.Nombre);
                    pgCommand.Parameters.AddWithValue("@apellido", request.Apellido);
                    pgCommand.Parameters.AddWithValue("@email", request.Email);
                    pgCommand.Parameters.AddWithValue("@contrasenaHash", BCrypt.Net.BCrypt.HashPassword(request.Contrasena));
                    pgCommand.Parameters.AddWithValue("@telefono", request.Telefono ?? (object)DBNull.Value);
                    pgCommand.Parameters.AddWithValue("@companyId", companyId);

                    int newUserId = (int)pgCommand.ExecuteScalar();

                    // --- 3. Insertar en MongoDB (user_sync) ---
                    var mongoCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                    var newUserSync = new BsonDocument
            {
                { "sql_user_id", newUserId },
                { "email", request.Email },
                { "role", "employee" },
                { "company_mongo_id", request.CompanyMongoId },
                { "active", true },
                { "last_sync", DateTime.UtcNow }
            };
                    mongoCollection.InsertOne(newUserSync);

                    pgTransaction.Commit();
                    return Ok(new { status = 0, message = "Usuario empleado creado", user_id = newUserId });
                }
                catch (Exception ex)
                {
                    pgTransaction.Rollback();
                    return StatusCode(500, new { status = 2, message = $"Error: {ex.Message}" });
                }
            }
        }

        [HttpPost("CreateCollectorUser")]
        public ActionResult CreateCollectorUser([FromBody] CreateCollectorRequest request)
        {
            using (var pgConnection = PostgreSqlConnection.GetConnection())
            using (var pgTransaction = pgConnection.BeginTransaction())
            {
                try
                {
                    // --- 1. Insertar en PostgreSQL (users) ---
                    var pgCommand = new NpgsqlCommand(
                        @"INSERT INTO users 
                  (username, nombre, apellido, email, contrasena_hash, role, telefono, active, created_at)
                  VALUES 
                  (@username, @nombre, @apellido, @email, @contrasenaHash, 'collector', @telefono, true, CURRENT_TIMESTAMP)
                  RETURNING user_id;",
                        pgConnection,
                        pgTransaction
                    );

                    pgCommand.Parameters.AddWithValue("@username", request.Username);
                    pgCommand.Parameters.AddWithValue("@nombre", request.Nombre);
                    pgCommand.Parameters.AddWithValue("@apellido", request.Apellido);
                    pgCommand.Parameters.AddWithValue("@email", request.Email);
                    pgCommand.Parameters.AddWithValue("@contrasenaHash", BCrypt.Net.BCrypt.HashPassword(request.Contrasena));
                    pgCommand.Parameters.AddWithValue("@telefono", request.Telefono ?? (object)DBNull.Value);

                    int newCollectorId = (int)pgCommand.ExecuteScalar();

                    // --- 2. Insertar en MongoDB (user_sync) ---
                    var mongoUserCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                    var newUserSync = new BsonDocument
            {
                { "sql_user_id", newCollectorId },
                { "email", request.Email },
                { "role", "collector" },
                { "active", true },
                { "last_sync", DateTime.UtcNow }
            };
                    mongoUserCollection.InsertOne(newUserSync);

                    // --- 3. Crear registro en MongoDB (assignments) ---
                    var mongoAssignmentCollection = MongoDbConnection.GetCollection<BsonDocument>("assignments");
                    var newAssignment = new BsonDocument
            {
                { "assignment_id", $"ASSIGN-{newCollectorId}" },
                { "collector_id", newCollectorId.ToString() },
                { "admin_id", "1" },  // ID fijo del admin
                { "companies", new BsonArray() },  // Array vacío
                { "status", "active" },
                { "assigned_at", DateTime.UtcNow }
            };
                    mongoAssignmentCollection.InsertOne(newAssignment);

                    pgTransaction.Commit();
                    return Ok(new { status = 0, message = "Usuario recolector creado", user_id = newCollectorId });
                }
                catch (Exception ex)
                {
                    pgTransaction.Rollback();
                    return StatusCode(500, new { status = 2, message = $"Error: {ex.Message}" });
                }
            }
        }

        [HttpGet("employees")]
        public ActionResult GetEmployeeUsers()
        {
            try
            {
                // 1. Obtener empleados desde PostgreSQL
                var pgCommand = new NpgsqlCommand(
                    @"SELECT user_id, username, nombre, apellido, email, telefono 
              FROM users 
              WHERE role = 'employee'",
                    PostgreSqlConnection.GetConnection()
                );

                var employees = new List<EmployeeUserResponse>();
                using (var reader = pgCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var employee = new EmployeeUserResponse
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Nombre = reader.GetString(2),
                            Apellido = reader.GetString(3),
                            Email = reader.GetString(4),
                            Telefono = reader.IsDBNull(5) ? null : reader.GetString(5)
                        };
                        employees.Add(employee);
                    }
                }

                // 2. Obtener company_mongo_id desde MongoDB (user_sync)
                var mongoCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                foreach (var employee in employees)
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("sql_user_id", employee.UserId);
                    var userSync = mongoCollection.Find(filter).FirstOrDefault();
                    if (userSync != null)
                    {
                        employee.CompanyMongoId = userSync.GetValue("company_mongo_id", "").AsString;
                    }
                }

                return Ok(new { status = 0, data = employees });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = ex.Message });
            }
        }

        [HttpGet("collectors")]
        public ActionResult GetCollectorUsers()
        {
            try
            {
                // 1. Obtener recolectores desde PostgreSQL
                var pgCommand = new NpgsqlCommand(
                    @"SELECT user_id, username, nombre, apellido, email, telefono 
              FROM users 
              WHERE role = 'collector'",
                    PostgreSqlConnection.GetConnection()
                );

                var collectors = new List<CollectorUserResponse>();
                using (var reader = pgCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var collector = new CollectorUserResponse
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Nombre = reader.GetString(2),
                            Apellido = reader.GetString(3),
                            Email = reader.GetString(4),
                            Telefono = reader.IsDBNull(5) ? null : reader.GetString(5)
                        };
                        collectors.Add(collector);
                    }
                }

                // 2. Obtener assignment_id desde MongoDB (assignments)
                var mongoCollection = MongoDbConnection.GetCollection<BsonDocument>("assignments");
                foreach (var collector in collectors)
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("collector_id", collector.UserId.ToString());
                    var assignment = mongoCollection.Find(filter).FirstOrDefault();
                    if (assignment != null)
                    {
                        collector.AssignmentId = assignment.GetValue("assignment_id", "").AsString;
                    }
                }

                return Ok(new { status = 0, data = collectors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = ex.Message });
            }
        }

        [HttpGet("CardActiveUsers")]
        public ActionResult<int> CountActiveUsers()
        {
            var collection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
            var filter = Builders<BsonDocument>.Filter.Eq("active", true);
            long count = collection.CountDocuments(filter);
            return Ok((int)count); // Convertir long a int para Swagger
        }

        [HttpGet("CardCollectors")]
        public ActionResult<int> CountCollectors()
        {
            var collection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
            var filter = Builders<BsonDocument>.Filter.Eq("role", "collector");
            long count = collection.CountDocuments(filter);
            return Ok((int)count);
        }

        [HttpGet("CardAdmins")]
        public ActionResult<int> CountAdmins()
        {
            var collection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
            var filter = Builders<BsonDocument>.Filter.Eq("role", "admin");
            long count = collection.CountDocuments(filter);
            return Ok((int)count);
        }


        // Método auxiliar para obtener company_id desde company_mongo_id
        private int? GetCompanyIdByMongoId(string mongoCompanyId)
        {
            var pgCommand = new NpgsqlCommand(
                "SELECT company_id FROM companies WHERE mongo_company_id = @mongoId",
                PostgreSqlConnection.GetConnection()
            );
            pgCommand.Parameters.AddWithValue("@mongoId", mongoCompanyId);
            return (int?)pgCommand.ExecuteScalar();
        }

        // Request/Response models
        public class AdminUpdateRequest
        {
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Contrasena { get; set; }
            public string Email { get; set; }
        }

        public class UserCombinedResponse
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            // Agrega más campos si son necesarios
        }

        public class UserUpdateRequest
        {
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            // public string Role { get; set; }
        }

        public class CreateEmployeeRequest
        {
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Contrasena { get; set; }
            public string Telefono { get; set; }
            public string CompanyMongoId { get; set; }  // ID de MongoDB (COMP-001)
        }

        public class CreateCollectorRequest
        {
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Contrasena { get; set; }
            public string Telefono { get; set; }
            // No incluir company_mongo_id
        }
        public class EmployeeUserResponse
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Telefono { get; set; }
            public string CompanyMongoId { get; set; }  // Desde MongoDB (user_sync)
        }

        public class CollectorUserResponse
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Email { get; set; }
            public string Telefono { get; set; }
            public string AssignmentId { get; set; }  // Desde MongoDB (assignments)
        }
    }
}