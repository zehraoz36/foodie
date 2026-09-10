using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Data;
using YemekApp.API.Dtos;
using YemekApp.API.Models;

namespace YemekApp.API.Controllers;

[ApiController]
[Route("api/favorites")]
public class FavoriteController(AppDbContext db, TimeProvider clock) : AuthenticatedController
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> List(ulong userId, CancellationToken ct)
    {
        if (!IsOwner(userId)) return Forbid();
        var favorites = await db.Favorites.AsNoTracking().Include(x => x.Restaurant)
            .Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Ok(favorites.Select(x => new { x.Id, x.UserId, x.RestaurantId, x.CreatedAt,
            Restaurant = RestaurantResponse.From(x.Restaurant) }));
    }

    [HttpPost]
    public async Task<IActionResult> Add(RestaurantActionRequest request, CancellationToken ct)
    {
        if (!IsOwner(request.UserId)) return Forbid();
        if (!await db.Restaurants.AnyAsync(x => x.Id == request.RestaurantId, ct))
            return Problem(statusCode: 404, title: "Restoran bulunamadı.");
        if (await db.Favorites.AnyAsync(x => x.UserId == CurrentUserId && x.RestaurantId == request.RestaurantId, ct))
            return Problem(statusCode: 409, title: "Restoran zaten favorilerinizde.");
        var favorite = new Favorite { UserId = CurrentUserId, RestaurantId = request.RestaurantId, CreatedAt = clock.GetUtcNow().UtcDateTime };
        db.Favorites.Add(favorite);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { favorite.Id, favorite.UserId, favorite.RestaurantId, favorite.CreatedAt });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(ulong id, CancellationToken ct)
    {
        var favorite = await db.Favorites.SingleOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId, ct);
        if (favorite is null) return Problem(statusCode: 404, title: "Favori bulunamadı.");
        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
