using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using YemekApp.API.Data;

namespace YemekApp.API.Tests;

// Testler gerçek yemek_app yerine kendilerine ait geçici MySQL veritabanını kullanır.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string database = "yemek_app_test_" + Guid.NewGuid().ToString("N");
    private string connection = "";
    private string adminConnection = "";
    private bool created;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(ApiDirectory());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseMySql(connection,
                new MySqlServerVersion(new Version(8, 0, 0))));
        });
    }

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("YEMEK_TEST_MYSQL");
        if (string.IsNullOrWhiteSpace(configured))
        {
            using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiDirectory(), "appsettings.Local.json")));
            configured = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
        }
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Test için yerel MySQL bağlantısı gereklidir.");
        var cs = new MySqlConnectionStringBuilder(configured) { Database = "" };
        adminConnection = cs.ConnectionString;
        await using var admin = new MySqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = admin.CreateCommand();
        command.CommandText = $"CREATE DATABASE `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
        await command.ExecuteNonQueryAsync();
        created = true;
        cs.Database = database;
        connection = cs.ConnectionString;
        try
        {
            await using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
        }
        catch
        {
            await DropDatabase();
            throw;
        }
    }

    public AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 0))).Options);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await DropDatabase();
    }

    private async Task DropDatabase()
    {
        if (!created) return;
        await using var admin = new MySqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = admin.CreateCommand();
        command.CommandText = $"DROP DATABASE `{database}`";
        await command.ExecuteNonQueryAsync();
        created = false;
    }

    private static string ApiDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "YemekApp.API", "YemekApp.API.csproj");
            if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Backend proje klasörü bulunamadı.");
    }
}
