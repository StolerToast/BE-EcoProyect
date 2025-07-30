namespace smartbin.Models.Incident
{
    public class PostIncident
    {
        public string IncidentId { get; set; }
        public string ContainerId { get; set; }
        public string CompanyId { get; set; }
        public int ReportedBy { get; set; } // Cambiado a int
        //public bool QrVerified { get; set; }
        public string QrScanId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public List<ImageInfo> Images { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }

        public class ImageInfo
        {
            public string Url { get; set; }
            public DateTime UploadedAt { get; set; }
        }
    }
}