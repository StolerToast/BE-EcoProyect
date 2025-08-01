using Microsoft.AspNetCore.Mvc;
using Npgsql;
using smartbin.DataAccess;
using System.Data;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostgreTestController : ControllerBase
    {
        [HttpGet("users/active")]
        public ActionResult GetActiveUsers()
        {
            try
            {
                // 1. Consulta SQL
                string query = @"
                    SELECT 
                        user_id as UserId,
                        username as Username,
                        nombre as Nombre,
                        apellido as Apellido,
                        email as Email,
                        role as Role
                    FROM users 
                    WHERE active = true 
                    LIMIT 10";

                // 2. Ejecutar consulta
                var command = new NpgsqlCommand(query);
                DataTable results = PostgreSqlConnection.ExecuteQuery(command);

                // 3. Convertir a formato JSON amigable
                var userList = results.AsEnumerable().Select(row => new
                {
                    UserId = row.Field<int>("UserId"),
                    Username = row.Field<string>("Username"),
                    NombreCompleto = $"{row.Field<string>("Nombre")} {row.Field<string>("Apellido")}",
                    Email = row.Field<string>("Email"),
                    Role = row.Field<string>("Role")
                }).ToList();

                return Ok(userList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = 1,
                    message = $"Error al consultar usuarios: {ex.Message}"
                });
            }
        }
    }
}