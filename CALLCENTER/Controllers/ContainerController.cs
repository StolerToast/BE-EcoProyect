using Microsoft.AspNetCore.Mvc;
using smartbin.Models.Container;
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
        public ActionResult GetAll()
        {
            var containers = Container.GetAll();
            return Ok(containers);
        }

        // GET: api/Container/CTN-001
        [HttpGet("{containerId}")]
        public ActionResult GetById(string containerId)
        {
            var container = Container.GetById(containerId);
            if (container == null)
                return NotFound();
            return Ok(container);
        }

        // POST: api/Container
        [HttpPost]
        public ActionResult Insert([FromForm] PostContainer postData)
        {
            var coordinates = postData.Location
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToArray();

            var container = new Container
            {
                ContainerId = postData.ContainerId,
                CompanyId = postData.CompanyId,
                QrCode = postData.QrCode,
                GeoLocation = new Location { Type = "Point", Coordinates = coordinates },
                Type = postData.Type,
                Capacity = postData.Capacity,
                Status = postData.Status,
                DeviceId = postData.DeviceId,
                LastCollection = DateTime.Parse(postData.LastCollection),
                CreatedAt = DateTime.UtcNow
            };

            container.Insert();
            return Ok(new { status = 0, message = "Contenedor creado correctamente" });
        }

        // PUT: api/Container/CTN-001
        [HttpPut("{containerId}")]
        public ActionResult Update(string containerId, [FromForm] PostContainer postData)
        {
            var existingContainer = Container.GetById(containerId);
            if (existingContainer == null)
                return NotFound();

            var coordinates = postData.Location
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToArray();

            var updatedContainer = new Container
            {
                Id = existingContainer.Id, // Mantener el mismo ObjectId
                ContainerId = postData.ContainerId,
                CompanyId = postData.CompanyId,
                QrCode = postData.QrCode,
                GeoLocation = new Location { Type = "Point", Coordinates = coordinates },
                Type = postData.Type,
                Capacity = postData.Capacity,
                Status = postData.Status,
                DeviceId = postData.DeviceId,
                LastCollection = DateTime.Parse(postData.LastCollection),
                CreatedAt = existingContainer.CreatedAt // No modificar
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