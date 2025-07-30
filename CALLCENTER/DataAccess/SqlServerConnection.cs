using smartbin.Config;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace smartbin.DataAccess
{
    public class SqlServerConnection
    {
        #region variables

        private static string GetConnectionString()
        {
            var config = AppConfigManager.Configuration.SqlServer;
            if (string.IsNullOrWhiteSpace(config.User) && string.IsNullOrWhiteSpace(config.Password))
            {
                // Autenticación de Windows
                return $"Data Source={config.Server};Initial Catalog={config.Database};Integrated Security=True;TrustServerCertificate=True;";
            }
            else
            {
                // Autenticación SQL
                return $"Data Source={config.Server};Initial Catalog={config.Database};User Id={config.User};Password={config.Password};TrustServerCertificate=True;";
            }
        }

        #endregion

        #region class methods

        private static SqlConnection GetConnection()
        {
            var connection = new SqlConnection(GetConnectionString());
            connection.Open();
            return connection;
        }

        public static DataTable ExecuteQuery(SqlCommand command)
        {
            DataTable table = new DataTable();
            using (SqlConnection connection = GetConnection())
            {
                try
                {
                    command.Connection = connection;
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
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

        public static bool ExecuteCommand(SqlCommand command) // Corregido el nombre aquí
        {
            using (SqlConnection connection = GetConnection())
            {
                try
                {
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing command: {ex.Message}", ex);
                }
            }
        }

        public static int ExecuteProcedure(SqlCommand command)
        {
            using (SqlConnection connection = GetConnection())
            {
                try
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter returnParameter = command.Parameters.Add("@status", SqlDbType.Int);
                    returnParameter.Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    object statusValue = command.Parameters["@status"].Value;
                    int result = (statusValue == DBNull.Value) ? -1 : (int)statusValue; // Manejo seguro

                    return result;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing procedure: {ex.Message}", ex);
                }
            }
        }

        public static int ExecuteNonQuery(SqlCommand command)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                try
                {
                    connection.Open();
                    command.Connection = connection;
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing non-query: {ex.Message}", ex);
                }
            }
        }

        #endregion
    }
}