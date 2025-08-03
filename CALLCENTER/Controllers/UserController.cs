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
    }
}