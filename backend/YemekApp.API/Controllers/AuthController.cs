using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Data;
using YemekApp.API.Dtos;
using YemekApp.API.Models;
using YemekApp.API.Services;

namespace YemekApp.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(AppDbContext db, IPasswordHasher<User> hasher, JwtService jwt, TimeProvider clock) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (username.Length < 3) return Problem(statusCode: 400, title: "Kullanıcı adı en az 3 karakter olmalıdır.");
        if (await db.Users.AnyAsync(x => x.Username == username || x.Email == email, ct))
            return Problem(statusCode: 409, title: "Kullanıcı adı veya e-posta zaten kayıtlı.");
        var user = new User { Username = username, Email = email, CreatedAt = clock.GetUtcNow().UtcDateTime };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, jwt.Create(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null) return Problem(statusCode: 401, title: "E-posta veya şifre hatalı.");
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Problem(statusCode: 401, title: "E-posta veya şifre hatalı.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
        }
        return Ok(jwt.Create(user));
    }
}
