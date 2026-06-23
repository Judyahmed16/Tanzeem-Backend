using System.ComponentModel.DataAnnotations.Schema;

namespace Tanzeem.Domain.Entities.Users
{
    public class UserSession
    {
        public int Id { get; set; }
        public string SessionKey { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

        public User User { get; set; } = default!;
    }
}
