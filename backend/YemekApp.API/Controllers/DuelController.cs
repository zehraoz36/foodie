using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Data;
using YemekApp.API.Dtos;
using YemekApp.API.Models;
using YemekApp.API.Services;

namespace YemekApp.API.Controllers;

[ApiController]
[Route("api/duel")]
public class DuelController(AppDbContext db, TimeProvider clock) : AuthenticatedController
{
    [AllowAnonymous]
    [HttpGet("weekly")]
    public async Task<IActionResult> Weekly(CancellationToken ct)
    {
        var week = WeekService.Current(clock.GetUtcNow().UtcDateTime);
        var results = await db.DuelVotes.AsNoTracking().Where(x => x.WeekNumber == week)
            .GroupBy(x => new { x.RestaurantId, x.Restaurant.Name })
            .Select(g => new { g.Key.RestaurantId, RestaurantName = g.Key.Name, VoteCount = g.Count() })
            .OrderByDescending(x => x.VoteCount).ThenBy(x => x.RestaurantId).ToListAsync(ct);
        return Ok(new { WeekNumber = week, Results = results });
    }

    [HttpPost("vote")]
    public async Task<IActionResult> Vote(RestaurantActionRequest request, CancellationToken ct)
    {
        if (!IsOwner(request.UserId)) return Forbid();
        if (!await db.Restaurants.AnyAsync(x => x.Id == request.RestaurantId, ct))
            return Problem(statusCode: 404, title: "Restoran bulunamadı.");
        var now = clock.GetUtcNow().UtcDateTime;
        var vote = new DuelVote { UserId = CurrentUserId, RestaurantId = request.RestaurantId,
            WeekNumber = WeekService.Current(now), CreatedAt = now };
        db.DuelVotes.Add(vote);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { vote.Id, vote.UserId, vote.RestaurantId, vote.WeekNumber, vote.CreatedAt });
    }
}
