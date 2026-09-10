namespace YemekApp.API.Models;

public class RestaurantCategory
{
    public ulong RestaurantId { get; set; }
    public ulong CategoryId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
