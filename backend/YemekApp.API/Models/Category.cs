namespace YemekApp.API.Models;

public class Category
{
    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public string? IconName { get; set; }
    public ICollection<RestaurantCategory> RestaurantCategories { get; set; } = new List<RestaurantCategory>();
}
