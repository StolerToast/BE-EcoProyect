using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using smartbin.DataAccess;
using System.Data;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPut("admin/update")]
        public ActionResult UpdateAdminUser([FromBody] AdminUpdateRequest request)
        {
            try
            {
                // --- 1. Actualizar en MongoDB (SQL_users) ---
                var mongoCollection = MongoDbConnection.GetCollection<BsonDocument>("SQL_users");
                var filter = Builders<BsonDocument>.Filter.Eq("user_id", 1); // ID del admin
                var update = Builders<BsonDocument>.Update
                    .Set("username", request.Username)
                    .Set("nombre", request.Nombre)
                    .Set("apellido", request.Apellido)
                    .Set("contrasena", request.Contrasena)
                    .Set("email", request.Email);

                var mongoResult = mongoCollection.UpdateOne(filter, update);

                // --- 2. Actualizar en MongoDB (user_sync) ---
                var syncCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                var syncFilter = Builders<BsonDocument>.Filter.Eq("sql_user_id", 1);
                var syncUpdate = Builders<BsonDocument>.Update
                    .Set("email", request.Email)
                    .Set("last_sync", DateTime.UtcNow);

                var syncResult = syncCollection.UpdateOne(syncFilter, syncUpdate);

                return Ok(new
                {
                    status = 0,
                    message = "Datos del admin actualizados en ambas colecciones",
                    sql_users_updated = mongoResult.ModifiedCount,
                    user_sync_updated = syncResult.ModifiedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 1,
                    message = $"Error: {ex.Message}"
                });
            }
        }
        [HttpGet("admin/validate")]
        public ActionResult ValidateAdminData()
        {
            try
            {
                // --- 1. Obtener datos de SQL_users ---
                var sqlUsersCollection = MongoDbConnection.GetCollection<BsonDocument>("SQL_users");
                var sqlUser = sqlUsersCollection.Find(Builders<BsonDocument>.Filter.Eq("user_id", 1)).FirstOrDefault();

                // --- 2. Obtener datos de user_sync ---
                var userSyncCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                var userSync = userSyncCollection.Find(Builders<BsonDocument>.Filter.Eq("sql_user_id", 1)).FirstOrDefault();

                if (sqlUser == null || userSync == null)
                {
                    return NotFound(new { status = 1, message = "Usuario admin no encontrado en una o ambas colecciones" });
                }

                // --- 3. Comparar datos clave ---
                bool isEmailConsistent = sqlUser["email"].AsString == userSync["email"].AsString;
                bool isRoleConsistent = sqlUser["role"].AsString == userSync["role"].AsString;

                return Ok(new
                {
                    status = 0,
                    sql_users_data = new
                    {
                        BsonValue = sqlUser["username"],
                        BsonValue1 = sqlUser["nombre"],
                        BsonValue2 = sqlUser["apellido"],
                        BsonValue3 = sqlUser["email"],
                        BsonValue4 = sqlUser["role"]
                    },
                    user_sync_data = new
                    {
                        BsonValue = userSync["email"],
                        BsonValue1 = userSync["role"],
                        last_sync = userSync["last_sync"].ToUniversalTime()
                    },
                    is_consistent = isEmailConsistent && isRoleConsistent,
                    inconsistencies = !isEmailConsistent ? "Email no coincide" :
                                     !isRoleConsistent ? "Rol no coincide" : "Ninguna"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 1,
                    message = $"Error al validar: {ex.Message}"
                });
            }
        }
        [HttpGet("{userId}")]
        public ActionResult GetUserCombined(int userId)
        {
            try
            {
                // 1. Obtener datos de SQL_users
                var sqlUsersCollection = MongoDbConnection.GetCollection<BsonDocument>("SQL_users");
                var sqlUser = sqlUsersCollection.Find(Builders<BsonDocument>.Filter.Eq("user_id", userId)).FirstOrDefault();

                if (sqlUser == null)
                    return NotFound(new { status = 1, message = "Usuario no encontrado en SQL_users" });

                // 2. Obtener datos de user_sync
                var userSyncCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                var userSync = userSyncCollection.Find(Builders<BsonDocument>.Filter.Eq("sql_user_id", userId)).FirstOrDefault();

                if (userSync == null)
                    return NotFound(new { status = 1, message = "Usuario no encontrado en user_sync" });

                // 3. Combinar datos
                var response = new UserCombinedResponse
                {
                    Nombre = sqlUser.GetValue("nombre", "").AsString,
                    Username = sqlUser.GetValue("username", "").AsString,
                    Password = sqlUser.GetValue("contrasena", "").AsString
                };

                return Ok(new { status = 0, data = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 2, message = $"Error: {ex.Message}" });
            }
        }


        // Modelo para el request
        public class AdminUpdateRequest
        {
            public string Username { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string Contrasena { get; set; }
            public string Email { get; set; }
        }
        // Modelo para GET (respuesta combinada)
        public class UserCombinedResponse
        {
            public string Nombre { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        // Modelo para PUT (actualización)
        public class UserUpdateRequest
        {
            public string Nombre { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; } // Requerido para sincronización
        }
    }
}