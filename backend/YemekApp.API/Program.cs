using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using YemekApp.API.Data;
using YemekApp.API.Infrastructure;
using YemekApp.API.Models;
using YemekApp.API.Services;

var builder = WebApplication.CreateBuilder(args);
// Yerel şifre dosyası Git dışında tutulur; ortam değişkenleri en yüksek önceliktedir.
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32 || string.IsNullOrWhiteSpace(jwt.Issuer)
    || string.IsNullOrWhiteSpace(jwt.Audience) || jwt.ExpirationMinutes is < 1 or > 1440)
    throw new InvalidOperationException("JWT ayarları eksik. Jwt:Key en az 32 bayt olmalıdır; yerel ayar dosyasını veya ortam değişkenlerini kullanınız.");
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.Configure<PasswordHasherOptions>(o => o.IterationCount = 210000);
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
    { Status = 400, Title = "İstek alanları geçersiz. Alan türlerini ve zorunlu bilgileri kontrol ediniz." });
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:8100").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            if (!ulong.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id) || id == 0)
            { context.Fail("Geçersiz kullanıcı kimliği."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await db.Users.AnyAsync(x => x.Id == id, context.HttpContext.RequestAborted))
                context.Fail("Kullanıcı bulunamadı.");
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 401, Title = "Geçerli bir JWT ile giriş yapınız." });
        },
        OnForbidden = context => context.Response.WriteAsJsonAsync(new ProblemDetails
        { Status = 403, Title = "Bu işlem için yetkiniz yok." })
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, ct) => await context.HttpContext.Response.WriteAsJsonAsync(
        new ProblemDetails { Status = 429, Title = "Çok fazla deneme yaptınız. Bir dakika sonra tekrar deneyiniz." }, ct);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Yemek Karar API", Version = "v1", Description = "Restoran keşfi, favoriler, çark ve düello API'si." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "Giriş yanıtındaki token değerini giriniz." });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});
var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
