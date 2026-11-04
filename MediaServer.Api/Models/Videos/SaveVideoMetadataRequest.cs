namespace MediaServer.Api.Models.Videos
{
    public class SaveVideoMetadataRequest
    {
        // Information returned by the file upload endpoint
        public string FileName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;

        // Video metadata
        public string Name { get; set; } = string.Empty;
        public int? SeriesId { get; set; }
        public string? PublisherName { get; set; }
        public string? PublicationDate { get; set; }
        public int Resolution { get; set; }
        public string? Synopsis { get; set; }
        public int Type { get; set; }
    }
}