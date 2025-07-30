using Microsoft.AspNetCore.Mvc;
using smartbin.Models.SensorData;
using smartbin.PostModels;
using System;
using System.Linq;

namespace smartbin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorDataController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get()
        {
            var list = SensorData.GetAll();
            return Ok(list);
        }

        [HttpGet("{deviceId}")]
        public ActionResult GetByDeviceId(string deviceId)
        {
            var data = SensorData.GetByDeviceId(deviceId);
            if (data == null)
                return NotFound();
            return Ok(data);
        }

        [HttpPost]
        public ActionResult Insert([FromForm] PostSensorData postData)
        {
            var coordinates = postData.Ubicacion
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToArray();

            var sensorData = new SensorData
            {
                DeviceId = postData.DeviceId,
                Timestamp = DateTime.Parse(postData.Timestamp),
                Temperatura = postData.Temperatura,
                Humedad = postData.Humedad,
                Metano = postData.Metano,
                CO2 = postData.CO2,
                NivelLlenado = postData.NivelLlenado,
                Ubicacion = new Ubicacion
                {
                    Type = "Point",
                    Coordinates = coordinates
                }
            };

            sensorData.Insert();
            return Ok(new { status = 0, message = "Sensor data insertado correctamente" });
        }
    }
}