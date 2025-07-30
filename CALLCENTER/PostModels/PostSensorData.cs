namespace smartbin.PostModels
{
    public class PostSensorData
    {
        public string DeviceId { get; set; }
        public string Timestamp { get; set; } // ISO string
        public double Temperatura { get; set; }
        public double Humedad { get; set; }
        public double Metano { get; set; }
        public double CO2 { get; set; }
        public double NivelLlenado { get; set; }
        public string Ubicacion { get; set; } // Ejemplo: "-99.123,19.456"
    }
}