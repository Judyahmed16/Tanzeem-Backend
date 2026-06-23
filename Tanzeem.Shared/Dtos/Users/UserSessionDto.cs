namespace Tanzeem.Shared.Dtos.Users
{
    public class UserSessionDto
    {
        public int Id { get; set; }
        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; }
    }
}
