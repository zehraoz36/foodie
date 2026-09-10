using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Dtos;
using YemekApp.API.Models;
using YemekApp.API.Services;

namespace YemekApp.API.Tests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Client, AuthResponse Auth, string Email)> Register()
    {
        var client = factory.CreateClient();
        var name = "test_" + Guid.NewGuid().ToString("N")[..12];
        var email = name + "@example.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { Username = name, Email = email, Password = "Test-parola-123!" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth, email);
    }

    private async Task<Restaurant> Restaurant(decimal lat = 41, decimal lng = 29)
    {
        await using var db = factory.CreateDb();
        var restaurant = new Restaurant { Name = "Test restoranı", City = "İstanbul", District = "Test", Address = "Test", Latitude = lat, Longitude = lng };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        return restaurant;
    }

    [Fact]
    public async Task KayitHashLoginVeYanlisSifre()
    {
        var (client, auth, email) = await Register();
        await using var db = factory.CreateDb();
        var stored = await db.Users.FindAsync(auth.UserId);
        Assert.NotNull(stored);
        Assert.NotEqual("Test-parola-123!", stored.PasswordHash);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Test-parola-123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.DoesNotContain("passwordHash", await login.Content.ReadAsStringAsync());
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "yanlis" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new { Username = stored.Username, Email = email, Password = "Test-parola-123!" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task FavoriSahipligiTekrarVeSilme()
    {
        var (client, auth, _) = await Register();
        var restaurant = await Restaurant();
        var add = await client.PostAsJsonAsync("/api/favorites", new { RestaurantId = restaurant.Id });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var favoriteId = (await add.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetUInt64();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/favorites", new { RestaurantId = restaurant.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/favorites/{auth.UserId + 1000}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/favorites", new { UserId = auth.UserId + 1000, RestaurantId = restaurant.Id })).StatusCode);
        var (other, _, _) = await Register();
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/favorites/{favoriteId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/favorites/{auth.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/favorites/{favoriteId}")).StatusCode);
    }

    [Fact]
    public async Task HaversineKategoriSinirVeSiralamasi()
    {
        var near = await Restaurant(41, 29);
        var next = await Restaurant(41.005m, 29);
        var far = await Restaurant(42, 30);
        await using (var db = factory.CreateDb())
        {
            var category = new Category { Name = "Soslu tavuk " + Guid.NewGuid().ToString("N")[..8] };
            foreach (var r in new[] { near, next, far })
                db.RestaurantCategories.Add(new RestaurantCategory { RestaurantId = r.Id, Category = category });
            await db.SaveChangesAsync();
            var slug = category.Name.ToLowerInvariant().Replace(' ', '-');
            var client = factory.CreateClient();
            var response = await client.GetAsync($"/api/restaurants?lat=41&lng=29&radius=2000&category={slug}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<List<RestaurantResponse>>())!;
            Assert.Equal(new[] { near.Id, next.Id }, result.Select(x => x.Id));
            Assert.Equal(0d, result[0].DistanceMeters);
            Assert.InRange(result[1].DistanceMeters!.Value, 550, 560);
            var page = await client.GetFromJsonAsync<List<RestaurantResponse>>($"/api/restaurants?lat=41&lng=29&radius=2000&category={slug}&page=2&pageSize=1");
            Assert.Equal(next.Id, Assert.Single(page!).Id);
        }
    }

    [Theory]
    [InlineData("lat=91&lng=29")]
    [InlineData("lat=41&lng=181")]
    [InlineData("lat=41&lng=29&radius=-1")]
    [InlineData("lat=NaN&lng=29")]
    [InlineData("lng=29")]
    public async Task GecersizKonum400(string query)
    {
        var response = await factory.CreateClient().GetAsync("/api/restaurants?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MenuDetayVeBulunamayanRestoran()
    {
        var r = await Restaurant();
        await using (var db = factory.CreateDb())
        {
            db.MenuItems.Add(new MenuItem { RestaurantId = r.Id, Name = "Test menü", Price = 125.50m, IsAvailable = false });
            await db.SaveChangesAsync();
        }
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/restaurants/{r.Id}")).StatusCode);
        var menu = await client.GetFromJsonAsync<JsonElement>($"/api/restaurants/{r.Id}/menu");
        Assert.Equal(125.50m, menu[0].GetProperty("price").GetDecimal());
        Assert.False(menu[0].GetProperty("isAvailable").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/restaurants/18446744073709551615/menu")).StatusCode);
    }

    [Fact]
    public async Task CarkVeHaftalikOy()
    {
        var (client, auth, _) = await Register();
        var r = await Restaurant();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/spin", new { ResultRestaurantId = r.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/spin", new { UserId = auth.UserId + 1, ResultRestaurantId = r.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/spin", new { ResultRestaurantId = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/duel/vote", new { RestaurantId = ulong.MaxValue })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/duel/vote", new { RestaurantId = r.Id })).StatusCode);
        var weekly = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/duel/weekly");
        Assert.Equal(WeekService.Current(DateTime.UtcNow), weekly.GetProperty("weekNumber").GetUInt32());
        Assert.Contains(weekly.GetProperty("results").EnumerateArray(), x => x.GetProperty("restaurantId").GetUInt64() == r.Id && x.GetProperty("voteCount").GetInt32() == 1);
    }

    [Theory]
    [InlineData("/api/favorites/1", "GET")]
    [InlineData("/api/favorites", "POST")]
    [InlineData("/api/favorites/1", "DELETE")]
    [InlineData("/api/spin", "POST")]
    [InlineData("/api/duel/vote", "POST")]
    public async Task TokensizErisim401(string path, string method)
    {
        var response = await factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerVeCors()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(swagger.GetProperty("paths").TryGetProperty("/api/restaurants", out _));
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/favorites");
        request.Headers.Add("Origin", "http://localhost:8100");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var response = await client.SendAsync(request);
        Assert.Equal("http://localhost:8100", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var blocked = new HttpRequestMessage(HttpMethod.Options, "/api/favorites");
        blocked.Headers.Add("Origin", "http://example.test");
        blocked.Headers.Add("Access-Control-Request-Method", "POST");
        Assert.False((await client.SendAsync(blocked)).Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task CascadeVeOnTablo()
    {
        var (client, auth, _) = await Register();
        var r = await Restaurant();
        await client.PostAsJsonAsync("/api/favorites", new { RestaurantId = r.Id });
        await using var db = factory.CreateDb();
        Assert.Equal(10, db.Model.GetEntityTypes().Count());
        // ExecuteDelete ile silme veritabanında gerçekleşir; EF'in istemci cascade'i kullanılmaz.
        await db.Restaurants.Where(x => x.Id == r.Id).ExecuteDeleteAsync();
        Assert.False(await db.Favorites.AnyAsync(x => x.UserId == auth.UserId && x.RestaurantId == r.Id));
    }

    [Fact]
    public void IsoHaftaYilGecisiVeHaversine()
    {
        Assert.Equal(202053u, WeekService.Current(new DateTime(2021, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(202101u, WeekService.Current(new DateTime(2021, 1, 3, 22, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(0, GeoService.Distance(41, 29, 41, 29));
        Assert.InRange(GeoService.Distance(0, 0, 0, 180), 20015000, 20016000);
    }
}
