namespace smartbin.PostModels
{
    public class PostContainer
    {
        public string CompanyId { get; set; }
        public string Type { get; set; } = "normal";
        public int Capacity { get; set; }
        public string Status { get; set; } = "active";
        public string DeviceId { get; set; } // Para obtener la ubicación del sensor
        //public string LastCollection { get; set; } // ISO string
    }
}