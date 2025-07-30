namespace smartbin.PostModels
{
    public class PostContainer
    {
        public string ContId { get; set; }
        public string DeviceId { get; set; }
        public string Ubicacion { get; set; } // Ejemplo: "-99.123,19.456"
        public string Estado { get; set; }
    }
}