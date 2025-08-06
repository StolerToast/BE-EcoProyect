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
        [HttpPost]
        public ActionResult Insert([FromForm] PostSensorData postData)
        {
            var coordinates = postData.Location
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s.Trim()))
                .ToArray();

            var sensorData = new SensorData
            {
                DeviceId = postData.DeviceId,
                ContainerId = postData.ContainerId, // Nuevo campo
                Timestamp = DateTime.Parse(postData.Timestamp),
                SensorReadings = new Readings
                {
                    Temperature = postData.Temperature,
                    Humidity = postData.Humidity,
                    Methane = postData.Methane,
                    CO2 = postData.CO2,
                    FillLevel = postData.FillLevel,
                    BatteryLevel = postData.BatteryLevel
                },
                PointLocation = new Location
                {
                    Type = "Point",
                    Coordinates = coordinates
                },
                Alerts = postData.Alerts // Nuevo campo
            };

            sensorData.Insert();
            return Ok(new { status = 0, message = "Datos del sensor insertados correctamente" });
        }

        [HttpGet]
        public ActionResult GetAll()
        {
            var list = SensorData.GetAll(); // Retorna List<SensorData> con la nueva estructura
            return Ok(list);
        }

        [HttpGet("GraphicTemperatureHumidity")]
        public ActionResult GetDashboardData()
        {
            try
            {
                var results = SensorData.GetLatestReadingsForDashboard()
                    .ConvertAll(doc => new
                    {
                        device_id = doc["device_id"].AsString,
                        temperature = doc["temperature"].AsDouble,
                        humidity = doc["humidity"].AsDouble,
                        timestamp = doc["timestamp"].ToUniversalTime()
                    });

                return Ok(new { status = 0, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = ex.Message });
            }
        }

        [HttpGet("dashboard/gases")]
        public ActionResult GetDashboardGasData()
        {
            try
            {
                var results = SensorData.GetLatestGasReadingsForDashboard()
                    .ConvertAll(doc => new
                    {
                        device_id = doc.GetValue("device_id", "").AsString,
                        methane = doc.GetValue("methane", 0.0).AsDouble,  // en ppm
                        co2 = doc.GetValue("co2", 0.0).AsDouble,          // en ppm
                        timestamp = doc.GetValue("timestamp", DateTime.MinValue).ToUniversalTime()
                    });

                return Ok(new { status = 0, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = ex.Message });
            }
        }
    }
}