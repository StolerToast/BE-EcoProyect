using Npgsql; // Paquete NuGet: Npgsql
using smartbin.Config;
using System;
using System.Data;

namespace smartbin.DataAccess
{
    public static class PostgreSqlConnection
    {
        private static string GetConnectionString()
        {
            var config = AppConfigManager.Configuration.PostgreSql; // Asegúrate de agregar PostgreSql en tu AppConfigManager
            return $"Server={config.Server};Port={config.Port};Database={config.Database};User Id={config.User};Password={config.Password};";
        }

        public static NpgsqlConnection GetConnection()
        {
            var connection = new NpgsqlConnection(GetConnectionString());
            connection.Open();
            return connection;
        }

        public static DataTable ExecuteQuery(NpgsqlCommand command)
        {
            DataTable table = new DataTable();
            using (var connection = GetConnection())
            {
                try
                {
                    command.Connection = connection;
                    using (var adapter = new NpgsqlDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing query: {ex.Message}", ex);
                }
            }
            return table;
        }

        public static int ExecuteNonQuery(NpgsqlCommand command)
        {
            using (var connection = GetConnection())
            {
                try
                {
                    command.Connection = connection;
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing command: {ex.Message}", ex);
                }
            }
        }
    }
}