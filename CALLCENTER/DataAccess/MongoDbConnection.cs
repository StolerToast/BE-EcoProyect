using MongoDB.Driver;
using smartbin.Config;
using System;

namespace smartbin.DataAccess
{
    public static class MongoDbConnection
    {
        private static IMongoDatabase? _database;
        private static MongoClient? _client;

        private static string GetConnectionString()
        {
            var config = AppConfigManager.Configuration.MongoDB;
            if (string.IsNullOrWhiteSpace(config?.ConnectionString))
                throw new InvalidOperationException("La cadena de conexión de MongoDB no está configurada.");
            return config.ConnectionString;
        }

        private static string GetDatabaseName()
        {
            var config = AppConfigManager.Configuration.MongoDB;
            if (string.IsNullOrWhiteSpace(config?.Database))
                throw new InvalidOperationException("El nombre de la base de datos de MongoDB no está configurado.");
            return config.Database;
        }

        private static void EnsureConnection()
        {
            if (_database == null)
            {
                var connectionString = GetConnectionString();
                var dbName = GetDatabaseName();
                _client = new MongoClient(connectionString);
                _database = _client.GetDatabase(dbName);
            }
        }

        public static IMongoDatabase GetDatabase()
        {
            EnsureConnection();
            return _database!;
        }

        public static IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            EnsureConnection();
            return _database!.GetCollection<T>(collectionName);
        }
    }
}