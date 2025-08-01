namespace smartbin.Config
{
    public class ConfigPostgreSql
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Port { get; set; } = 5432; // Puerto por defecto de PostgreSQL
    }
}