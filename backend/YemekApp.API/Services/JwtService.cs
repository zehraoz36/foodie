using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using YemekApp.API.Dtos;
using YemekApp.API.Models;

namespace YemekApp.API.Services;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = "YemekApp.API";
    public string Audience { get; set; } = "YemekApp.Frontend";
    public string Key { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class JwtService(JwtSettings settings, TimeProvider clock)
{
    public AuthResponse Create(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.ExpirationMinutes);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
             new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            now, expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)), SecurityAlgorithms.HmacSha256));
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Id, user.Username);
    }
}
