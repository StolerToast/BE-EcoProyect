namespace smartbin.PostModels
{
    public class PostContainer
    {
        public string ContainerId { get; set; }
        public string CompanyId { get; set; }
        public string QrCode { get; set; }
        public string Location { get; set; } // Formato: "-99.123,19.456"
        public string Type { get; set; } = "normal";
        public int Capacity { get; set; }
        public string Status { get; set; } = "active";
        public string DeviceId { get; set; }
        public string LastCollection { get; set; } // ISO 8601 string
    }
}