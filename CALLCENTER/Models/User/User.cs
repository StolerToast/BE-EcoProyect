using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using smartbin.DataAccess;
using System;
using System.Data;

namespace smartbin.Models.User
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string ContrasenaHash { get; set; }
        public string Role { get; set; }

        // Método para actualizar un usuario en PostgreSQL y MongoDB de forma atómica
        public static bool UpdateUserWithSync(int userId, string username, string nombre, string apellido, string email, string contrasenaHash)
        {
            using (var pgConnection = PostgreSqlConnection.GetConnection())
            using (var pgTransaction = pgConnection.BeginTransaction())
            {
                try
                {
                    var pgCommand = new NpgsqlCommand(@"
                UPDATE users 
                SET username = @username, 
                    nombre = @nombre, 
                    apellido = @apellido, 
                    email = @email, 
                    contrasena_hash = @contrasena_hash
                WHERE user_id = @user_id", pgConnection, pgTransaction);

                    pgCommand.Parameters.AddWithValue("@username", username);
                    pgCommand.Parameters.AddWithValue("@nombre", nombre);
                    pgCommand.Parameters.AddWithValue("@apellido", apellido);
                    pgCommand.Parameters.AddWithValue("@email", email);
                    pgCommand.Parameters.AddWithValue("@contrasena_hash", contrasenaHash);
                    pgCommand.Parameters.AddWithValue("@user_id", userId);

                    int pgRowsAffected = pgCommand.ExecuteNonQuery();
                    if (pgRowsAffected == 0)
                    {
                        pgTransaction.Rollback();
                        return false;
                    }

                    // Actualizar MongoDB (opcional)
                    var mongoUpdate = Builders<BsonDocument>.Update
                        .Set("email", email)
                        .Set("last_sync", DateTime.UtcNow);

                    var mongoResult = MongoDbConnection.GetCollection<BsonDocument>("user_sync")
                        .UpdateOne(
                            Builders<BsonDocument>.Filter.Eq("sql_user_id", userId),
                            mongoUpdate
                        );

                    pgTransaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    pgTransaction.Rollback();
                    throw new Exception($"Error al actualizar usuario: {ex.Message}");
                }
            }
        }
        public static (User pgUser, BsonDocument mongoSync) GetUserCombined(int userId)
        {
            // 1. Obtener de PostgreSQL
            var pgCommand = new NpgsqlCommand("SELECT * FROM users WHERE user_id = @userId", PostgreSqlConnection.GetConnection());
            pgCommand.Parameters.AddWithValue("@userId", userId);

            using (var reader = pgCommand.ExecuteReader())
            {
                if (!reader.Read())
                    throw new Exception("Usuario no encontrado en PostgreSQL");

                var pgUser = new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Nombre = reader.GetString(2),
                    Apellido = reader.GetString(3),
                    Email = reader.GetString(4),
                    ContrasenaHash = reader.GetString(5),
                    Role = reader.GetString(6)
                };

                // 2. Obtener de MongoDB (user_sync)
                var mongoCollection = MongoDbConnection.GetCollection<BsonDocument>("user_sync");
                var mongoSync = mongoCollection.Find(Builders<BsonDocument>.Filter.Eq("sql_user_id", userId)).FirstOrDefault();

                return (pgUser, mongoSync);
            }
        }
    }
}