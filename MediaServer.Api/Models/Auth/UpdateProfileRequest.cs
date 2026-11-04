namespace MediaServer.Api.Models.Auth
{
    public class UpdateProfileRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Picture { get; set; }
    }
}
