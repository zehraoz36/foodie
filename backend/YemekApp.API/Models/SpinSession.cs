namespace YemekApp.API.Models;

public class SpinSession
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong? ResultRestaurantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Restaurant? ResultRestaurant { get; set; }
}
