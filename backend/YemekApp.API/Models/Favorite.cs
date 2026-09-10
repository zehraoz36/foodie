namespace YemekApp.API.Models;

public class Favorite
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong RestaurantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Restaurant Restaurant { get; set; } = null!;
}
