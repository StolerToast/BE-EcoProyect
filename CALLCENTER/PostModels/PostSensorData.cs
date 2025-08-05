namespace smartbin.PostModels
{
    public class PostSensorData
    {
        public string DeviceId { get; set; }
        public string ContainerId { get; set; } // Nuevo campo
        public string Timestamp { get; set; } // ISO string
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Methane { get; set; }
        public double CO2 { get; set; }
        public double FillLevel { get; set; }
        public double BatteryLevel { get; set; }
        public string Location { get; set; } // Formato: "-99.123,19.456"
        public List<string> Alerts { get; set; } = new List<string>(); // Nuevo campo
    }
}