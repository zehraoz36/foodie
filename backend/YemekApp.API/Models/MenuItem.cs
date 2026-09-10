namespace YemekApp.API.Models;

public class MenuItem
{
    public ulong Id { get; set; }
    public ulong RestaurantId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public Restaurant Restaurant { get; set; } = null!;
}
