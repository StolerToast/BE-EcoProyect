using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace smartbin.Config
{
    public class Config
    {
        public ConfigSqlServer SqlServer { get; set; }
        public ConfigPaths Paths { get; set; }
        public ConfigMongoDb MongoDB { get; set; }
        public ConfigPostgreSql PostgreSql { get; set; } // Nueva propiedad
    }
}
