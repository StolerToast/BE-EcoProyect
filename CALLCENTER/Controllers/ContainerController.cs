using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using smartbin.Models.Container;
using smartbin.PostModels;
using System.Linq;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContainerController : ControllerBase
    {
        [HttpGet]
        [Route("")]
        public ActionResult Get()
        {
            List<Container> list = Container.Get();
            return Ok(ContainerListViewModel.GetResponse(list));
        }

        [HttpGet("{contId}")]
        public ActionResult GetByContId(string contId)
        {
            var container = Container.GetByContId(contId);
            if (container == null)
                return NotFound();
            return Ok(container);
        }

        [HttpPost]
        public ActionResult Insert([FromForm] PostContainer newContainer)
        {
            // Convertir la ubicación a array de double
            var ubicacion = newContainer.Ubicacion
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToArray();

            var container = new Container(newContainer.ContId, newContainer.DeviceId, ubicacion, newContainer.Estado);
            container.Insert();
            return Ok(new { status = 0, message = "Contenedor insertado correctamente" });
        }
    }
}
