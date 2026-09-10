using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Data;
using YemekApp.API.Dtos;
using YemekApp.API.Services;

namespace YemekApp.API.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Nearby([FromQuery] NearbyRequest request, CancellationToken ct)
    {
        var lat = request.Lat!.Value;
        var lng = request.Lng!.Value;
        if (!double.IsFinite(lat) || !double.IsFinite(lng))
            return Problem(statusCode: 400, title: "Koordinatlar sonlu sayılar olmalıdır.");
        var latDelta = request.Radius / GeoService.EarthRadius * 180 / Math.PI;
        // Parametreli SQL: Haversine filtresi ve sıralama MySQL üzerinde çalışır.
        // Enlem ön filtresi koordinat indeksini kullanabilmek için eklenmiştir.
        var query = db.Restaurants.FromSqlInterpolated($"""
            SELECT r.* FROM restaurant AS r
            WHERE r.latitude BETWEEN {lat - latDelta} AND {lat + latDelta}
              AND 2 * 6371000 * ASIN(SQRT(LEAST(1.0, GREATEST(0.0,
                POWER(SIN(RADIANS(r.latitude - {lat}) / 2), 2)
                + COS(RADIANS({lat})) * COS(RADIANS(r.latitude))
                * POWER(SIN(RADIANS(r.longitude - {lng}) / 2), 2))))) <= {request.Radius}
            """).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            // Şemada slug sütunu yoktur: soslu-tavuk, 'Soslu tavuk' adına karşılık gelir.
            var name = request.Category.Trim().Replace('-', ' ');
            query = query.Where(r => r.RestaurantCategories.Any(c => c.Category.Name == name));
        }
        // Küresel kosinüs değeri mesafeyle ters sıralıdır; Haversine filtresi yukarıdadır.
        var restaurants = await query.OrderByDescending(r =>
            Math.Sin(lat * Math.PI / 180) * Math.Sin((double)r.Latitude * Math.PI / 180)
            + Math.Cos(lat * Math.PI / 180) * Math.Cos((double)r.Latitude * Math.PI / 180)
            * Math.Cos(((double)r.Longitude - lng) * Math.PI / 180))
            .ThenBy(r => r.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return Ok(restaurants.Select(r => RestaurantResponse.From(r,
            Math.Round(GeoService.Distance(lat, lng, (double)r.Latitude, (double)r.Longitude), 2))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detail(ulong id, CancellationToken ct)
    {
        var restaurant = await db.Restaurants.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        if (restaurant is null) return Problem(statusCode: 404, title: "Restoran bulunamadı.");
        var categories = await db.RestaurantCategories.Where(x => x.RestaurantId == id)
            .Select(x => new { x.Category.Id, x.Category.Name, x.Category.IconName }).ToListAsync(ct);
        return Ok(new { Restaurant = RestaurantResponse.From(restaurant), Categories = categories });
    }

    [HttpGet("{id}/menu")]
    public async Task<IActionResult> Menu(ulong id, CancellationToken ct)
    {
        if (!await db.Restaurants.AnyAsync(r => r.Id == id, ct))
            return Problem(statusCode: 404, title: "Restoran bulunamadı.");
        return Ok(await db.MenuItems.AsNoTracking().Where(x => x.RestaurantId == id).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.RestaurantId, x.Name, x.Description, x.Price, x.IsAvailable }).ToListAsync(ct));
    }
}
