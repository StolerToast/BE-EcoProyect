using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using smartbin.DataAccess;
using smartbin.Models.Container;
using smartbin.Models.SensorData;
using smartbin.PostModels;
using System;
using System.Linq;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContainerController : ControllerBase
    {
        // GET: api/Container
        [HttpGet]
        public ActionResult GetAll() => Ok(Container.GetAll());

        [HttpGet("{containerId}")]
        public ActionResult GetById(string containerId)
        {
            var container = Container.GetById(containerId);
            return container != null ? Ok(container) : NotFound();
        }

        [HttpGet("by-company/{companyId}")]
        public ActionResult GetByCompanyId(string companyId)
        {
            var collection = MongoDbConnection.GetCollection<Container>("containers");
            var filter = Builders<Container>.Filter.Eq(c => c.CompanyId, companyId);
            var containers = collection.Find(filter).ToList();
            return Ok(containers);
        }

        // POST: api/Container
        [HttpPost]
        public ActionResult Insert([FromForm] PostContainer postData)
        {
            // 1. Obtener la ubicación del device_id desde sensor_data
            var sensorCollection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
            var latestSensorData = sensorCollection.Find(s => s.DeviceId == postData.DeviceId)
                                                 .SortByDescending(s => s.Timestamp)
                                                 .FirstOrDefault();

            if (latestSensorData == null)
                return BadRequest("El dispositivo no tiene datos de ubicación registrados");

            // 2. Generar container_id automático
            string containerId = Container.GetNextContainerId();
            var currentDate = DateTime.UtcNow; // Fecha/hora actual para created_at y last_collection

            // 3. Crear el contenedor
            var container = new Container
            {
                ContainerId = containerId,
                CompanyId = postData.CompanyId,
                QrCode = "", // QR vacío
                GeoLocation = new Models.Container.Location
                {
                    Type = "Point",
                    Coordinates = latestSensorData.PointLocation.Coordinates
                },
                Type = postData.Type,
                Capacity = postData.Capacity,
                Status = postData.Status,
                DeviceId = postData.DeviceId,
                LastCollection = currentDate, // Igual que created_at
                CreatedAt = currentDate
            };

            container.Insert();
            return Ok(new { status = 0, message = "Contenedor creado", container_id = containerId });
        }

        // PUT: api/Container/CTN-001
        [HttpPut("{containerId}")]
        public ActionResult Update(string containerId, [FromForm] PostContainer postData)
        {
            var existingContainer = Container.GetById(containerId);
            if (existingContainer == null)
                return NotFound();

            // Obtener nueva ubicación si el device_id cambió
            double[] coordinates = existingContainer.GeoLocation.Coordinates;
            if (postData.DeviceId != existingContainer.DeviceId)
            {
                var sensorCollection = MongoDbConnection.GetCollection<SensorData>("sensor_data");
                var latestSensorData = sensorCollection.Find(s => s.DeviceId == postData.DeviceId)
                                                     .SortByDescending(s => s.Timestamp)
                                                     .FirstOrDefault();
                if (latestSensorData != null)
                    coordinates = latestSensorData.PointLocation.Coordinates;
            }

            var updatedContainer = new Container
            {
                Id = existingContainer.Id,
                ContainerId = containerId, // No cambia
                CompanyId = postData.CompanyId,
                QrCode = existingContainer.QrCode, // Mantener el QR original
                GeoLocation = new Models.Container.Location { Type = "Point", Coordinates = coordinates },
                Type = postData.Type,
                Capacity = postData.Capacity,
                Status = postData.Status,
                DeviceId = postData.DeviceId,
                //LastCollection = DateTime.Parse(postData.LastCollection),
                CreatedAt = existingContainer.CreatedAt
            };

            bool success = Container.Update(containerId, updatedContainer);
            return success ? Ok(new { status = 0, message = "Contenedor actualizado" }) : BadRequest();
        }

        // DELETE: api/Container/CTN-001
        [HttpDelete("{containerId}")]
        public ActionResult Delete(string containerId)
        {
            bool success = Container.Delete(containerId);
            return success ? Ok(new { status = 0, message = "Contenedor eliminado" }) : NotFound();
        }

        [HttpGet("CardContainers")]
        public ActionResult GetActiveContainersCount()
        {
            long activeCount = Container.CountActiveContainers();
            return Ok(new { active_containers = activeCount });
        }
    }
}