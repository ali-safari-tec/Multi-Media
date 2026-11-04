namespace MediaServer.Api.Models.Common
{
    public class AuthenticatedUser
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int AccessLevel { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }
}
