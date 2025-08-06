using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

using smartbin.DataAccess;
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
        [HttpGet("latest")]
        public ActionResult GetLatestReadings()
        {
            try
            {
                var results = SensorData.GetLatestReadingsPerDevice()
                    .ConvertAll(doc => new
                    {
                        device_id = doc.GetValue("device_id", "").AsString,
                        container_id = doc.GetValue("container_id", "").AsString,
                        timestamp = doc.GetValue("timestamp", DateTime.MinValue).ToUniversalTime(),
                        readings = new
                        {
                            temperature = doc["readings"].AsBsonDocument.GetValue("temperature", 0.0).AsDouble,
                            humidity = doc["readings"].AsBsonDocument.GetValue("humidity", 0.0).AsDouble,
                            methane = doc["readings"].AsBsonDocument.GetValue("methane", 0.0).AsDouble,
                            co2 = doc["readings"].AsBsonDocument.GetValue("co2", 0.0).AsDouble,
                            fill_level = doc["readings"].AsBsonDocument.GetValue("fill_level", 0.0).AsDouble,
                            battery_level = doc["readings"].AsBsonDocument.GetValue("battery_level", 0.0).AsDouble
                        }
                    });

                return Ok(new { status = 0, data = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 1, message = ex.Message });
            }
        }

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

        [HttpGet("dashboard/Temperature")]
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

        [HttpGet("dashboard/Gases")]
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

        [HttpGet("dashboard/Averages")]
        public ActionResult GetAverages()
        {
            var collection = MongoDbConnection.GetCollection<SensorData>("sensor_data");

            var pipeline = new[]
            {
        // Paso 1: Ordenar por device_id y timestamp (más reciente primero)
        new BsonDocument("$sort",
            new BsonDocument
            {
                { "device_id", 1 },
                { "timestamp", -1 }
            }),
        
        // Paso 2: Agrupar por device_id y quedarse con el primer registro
        new BsonDocument("$group",
            new BsonDocument
            {
                { "_id", "$device_id" },
                { "latest_doc", new BsonDocument("$first", "$$ROOT") }
            }),
        
        // Paso 3: Calcular promedios
        new BsonDocument("$group",
            new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "avg_temperature", new BsonDocument("$avg", "$latest_doc.readings.temperature") },
                { "avg_humidity", new BsonDocument("$avg", "$latest_doc.readings.humidity") },
                { "avg_methane", new BsonDocument("$avg", "$latest_doc.readings.methane") },
                { "avg_co2", new BsonDocument("$avg", "$latest_doc.readings.co2") },
                { "avg_fill_level", new BsonDocument("$avg", "$latest_doc.readings.fill_level") },
                { "devices_count", new BsonDocument("$sum", 1) }
            }),
        
        // Paso 4: Dar formato al resultado
        new BsonDocument("$project",
            new BsonDocument
            {
                { "_id", 0 },
                { "avg_temperature", new BsonDocument("$round", new BsonArray { "$avg_temperature", 2 }) },
                { "avg_humidity", new BsonDocument("$round", new BsonArray { "$avg_humidity", 2 }) },
                { "avg_methane", new BsonDocument("$round", new BsonArray { "$avg_methane", 2 }) },
                { "avg_co2", new BsonDocument("$round", new BsonArray { "$avg_co2", 2 }) },
                { "avg_fill_level", new BsonDocument("$round", new BsonArray { "$avg_fill_level", 2 }) },
                { "devices_count", 1 }
            })
    };

            var result = collection.Aggregate<SensorAverages>(pipeline).FirstOrDefault();

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}