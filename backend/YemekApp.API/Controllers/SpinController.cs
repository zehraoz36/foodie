using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Data;
using YemekApp.API.Dtos;
using YemekApp.API.Models;

namespace YemekApp.API.Controllers;

[ApiController]
[Route("api/spin")]
public class SpinController(AppDbContext db, TimeProvider clock) : AuthenticatedController
{
    [HttpPost]
    public async Task<IActionResult> Save(SpinRequest request, CancellationToken ct)
    {
        if (!IsOwner(request.UserId)) return Forbid();
        if (!await db.Restaurants.AnyAsync(x => x.Id == request.ResultRestaurantId, ct))
            return Problem(statusCode: 404, title: "Sonuç restoranı bulunamadı.");
        var session = new SpinSession { UserId = CurrentUserId, ResultRestaurantId = request.ResultRestaurantId,
            CreatedAt = clock.GetUtcNow().UtcDateTime };
        db.SpinSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { session.Id, session.UserId, session.ResultRestaurantId, session.CreatedAt });
    }
}
