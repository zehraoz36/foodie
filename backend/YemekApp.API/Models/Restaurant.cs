namespace YemekApp.API.Models;

public class Restaurant
{
    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public string District { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public byte? PriceLevel { get; set; }
    public decimal? Rating { get; set; }
    public string? Phone { get; set; }
    public bool IsOpen { get; set; } = true;
    public ICollection<RestaurantCategory> RestaurantCategories { get; set; } = new List<RestaurantCategory>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
