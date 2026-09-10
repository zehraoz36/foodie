using YemekApp.API.Models;

namespace YemekApp.API.Dtos;

public sealed record RestaurantResponse(ulong Id, string Name, string District, string City,
    string Address, decimal Latitude, decimal Longitude, byte? PriceLevel, decimal? Rating,
    string? Phone, bool IsOpen, double? DistanceMeters = null)
{
    public static RestaurantResponse From(Restaurant r, double? distance = null) =>
        new(r.Id, r.Name, r.District, r.City, r.Address, r.Latitude, r.Longitude,
            r.PriceLevel, r.Rating, r.Phone, r.IsOpen, distance);
}
public sealed record AuthResponse(string Token, DateTime ExpiresAt, ulong UserId, string Username);
