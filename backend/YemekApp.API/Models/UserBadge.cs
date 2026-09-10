namespace YemekApp.API.Models;

public class UserBadge
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong BadgeId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
