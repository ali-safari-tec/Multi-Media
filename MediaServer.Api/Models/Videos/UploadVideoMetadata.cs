namespace MediaServer.Api.Models.Videos
{
    public class UploadVideoMetadata
    {
        public string Name { get; set; } = string.Empty;

        public int? SeriesId { get; set; }

        public string? PublisherName { get; set; }

        public string? PublicationDate { get; set; }

        public int Resolution { get; set; }

        public string? Synopsis { get; set; }

        public int Type { get; set; }
    }
}
