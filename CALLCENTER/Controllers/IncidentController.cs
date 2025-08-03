using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using smartbin.Models.Incident;
using smartbin.DataAccess;
using MongoDB.Bson;
using System.Collections.Generic;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentController : ControllerBase
    {
        [HttpGet("general")]
        public ActionResult GetGeneral()
        {
            var list = Incident.GetGeneral();
            return Ok(list);
        }

        [HttpGet("specific")]
        public ActionResult GetSpecific()
        {
            var db = MongoDbConnection.GetDatabase();
            var list = Incident.GetSpecificIncidents(db).ConvertAll(doc => doc.ToDictionary());
            return Ok(list);
        }

        [HttpGet("resolved")]
        public ActionResult GetResolved()
        {
            var db = MongoDbConnection.GetDatabase();
            var list = Incident.GetSolvedIncidents(db).ConvertAll(doc => doc.ToDictionary());
            return Ok(list);
        }

        [HttpGet("graphic")]
        public ActionResult GetGraphic()
        {
            var db = MongoDbConnection.GetDatabase();
            var list = Incident.GetGraphicIncident(db).ConvertAll(doc => doc.ToDictionary());
            return Ok(list);
        }

        [HttpGet("recent")]
        public ActionResult GetRecent()
        {
            var db = MongoDbConnection.GetDatabase();
            var list = Incident.GetRecentReport(db).ConvertAll(doc => doc.ToDictionary());
            return Ok(list);
        }

        // Dentro de IncidentController.cs
        [HttpPatch("resolve/{incident_id}")]
        public ActionResult ResolveIncident(string incident_id, [FromBody] ResolutionRequest request)
        {
            try
            {
                var resolved = Incident.ResolveIncident(incident_id, request?.ResolutionNotes);

                if (!resolved)
                {
                    return NotFound(new { status = 1, message = "Incidente no encontrado o ya resuelto." });
                }

                return Ok(new { status = 0, message = "Incidente marcado como resuelto." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 2, message = $"Error interno: {ex.Message}" });
            }
        }

        // Clase auxiliar para el cuerpo de la solicitud (opcional)
        public class ResolutionRequest
        {
            public string ResolutionNotes { get; set; }
        }

        [HttpPost]
        public ActionResult Insert([FromBody] PostIncident newIncident)
        {
            var incident = new Incident
            {
                IncidentId = newIncident.IncidentId,
                ContainerId = newIncident.ContainerId,
                CompanyId = newIncident.CompanyId,
                ReportedBy = newIncident.ReportedBy,
                //QrVerified = newIncident.QrVerified, // Asignación directa, ambos son bool
                QrScanId = newIncident.QrScanId,
                Title = newIncident.Title,
                Description = newIncident.Description,
                Type = newIncident.Type,
                Priority = newIncident.Priority,
                Status = newIncident.Status,
                Images = newIncident.Images?.ConvertAll(i => new Incident.ImageInfo { Url = i.Url, UploadedAt = i.UploadedAt }) ?? new(),
                CreatedAt = newIncident.CreatedAt,
                ResolvedAt = newIncident.ResolvedAt,
                ResolutionNotes = newIncident.ResolutionNotes
            };
            var collection = MongoDbConnection.GetCollection<Incident>("incidents");
            collection.InsertOne(incident);
            return Ok(new { status = 0, message = "Incidente insertado correctamente" });
        }
    }
}
