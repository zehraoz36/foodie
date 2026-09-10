using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using YemekApp.API.Models;

namespace YemekApp.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RestaurantCategory> RestaurantCategories => Set<RestaurantCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<SpinSession> SpinSessions => Set<SpinSession>();
    public DbSet<DuelVote> DuelVotes => Set<DuelVote>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasCharSet("utf8mb4").UseCollation("utf8mb4_unicode_ci");
        var restaurant = model.Entity<Restaurant>();
        restaurant.Property(x => x.Name).HasMaxLength(200);
        restaurant.Property(x => x.City).HasMaxLength(100);
        restaurant.Property(x => x.District).HasMaxLength(100);
        restaurant.Property(x => x.Address).HasMaxLength(500);
        restaurant.Property(x => x.Latitude).HasPrecision(10, 8);
        // Türkiye boylamları iki tam basamaktan büyüktür.
        restaurant.Property(x => x.Longitude).HasPrecision(11, 8);
        restaurant.Property(x => x.Rating).HasPrecision(3, 2);
        restaurant.Property(x => x.Phone).HasMaxLength(25);
        restaurant.Property(x => x.IsOpen).HasDefaultValue(true);
        restaurant.HasIndex(x => new { x.City, x.District }).HasDatabaseName("idx_restaurant_city_district");
        restaurant.HasIndex(x => x.District).HasDatabaseName("idx_restaurant_district");
        restaurant.HasIndex(x => new { x.Latitude, x.Longitude }).HasDatabaseName("idx_restaurant_latitude_longitude");
        restaurant.HasIndex(x => x.Longitude).HasDatabaseName("idx_restaurant_longitude");

        var category = model.Entity<Category>();
        category.Property(x => x.Name).HasMaxLength(100);
        category.Property(x => x.IconName).HasMaxLength(100);
        category.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_category_name");
        var rc = model.Entity<RestaurantCategory>();
        rc.HasKey(x => new { x.RestaurantId, x.CategoryId });
        rc.HasIndex(x => x.CategoryId).HasDatabaseName("idx_restaurant_category_category");
        rc.HasOne(x => x.Restaurant).WithMany(x => x.RestaurantCategories).HasForeignKey(x => x.RestaurantId).HasConstraintName("fk_restaurant_category_restaurant");
        rc.HasOne(x => x.Category).WithMany(x => x.RestaurantCategories).HasForeignKey(x => x.CategoryId).HasConstraintName("fk_restaurant_category_category");

        var menu = model.Entity<MenuItem>();
        menu.Property(x => x.Name).HasMaxLength(200);
        menu.Property(x => x.Description).HasColumnType("text");
        menu.Property(x => x.Price).HasPrecision(10, 2);
        menu.Property(x => x.IsAvailable).HasDefaultValue(true);
        menu.HasIndex(x => new { x.RestaurantId, x.IsAvailable }).HasDatabaseName("idx_menu_item_restaurant_available");
        menu.HasOne(x => x.Restaurant).WithMany(x => x.MenuItems).HasForeignKey(x => x.RestaurantId).HasConstraintName("fk_menu_item_restaurant");

        var user = model.Entity<User>();
        user.Property(x => x.Username).HasMaxLength(50);
        user.Property(x => x.Email).HasMaxLength(254);
        user.Property(x => x.PasswordHash).HasMaxLength(512);
        user.HasIndex(x => x.Username).IsUnique().HasDatabaseName("uq_user_username");
        user.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_user_email");

        var favorite = model.Entity<Favorite>();
        favorite.HasIndex(x => new { x.UserId, x.RestaurantId }).IsUnique().HasDatabaseName("uq_favorite_user_restaurant");
        favorite.HasIndex(x => x.RestaurantId).HasDatabaseName("idx_favorite_restaurant");
        favorite.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("idx_favorite_user_created");
        favorite.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_favorite_user");
        favorite.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).HasConstraintName("fk_favorite_restaurant");

        var spin = model.Entity<SpinSession>();
        spin.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("idx_spin_session_user_created");
        spin.HasIndex(x => x.ResultRestaurantId).HasDatabaseName("idx_spin_session_result_restaurant");
        spin.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_spin_session_user");
        spin.HasOne(x => x.ResultRestaurant).WithMany().HasForeignKey(x => x.ResultRestaurantId).HasConstraintName("fk_spin_session_restaurant");

        var vote = model.Entity<DuelVote>();
        vote.HasIndex(x => new { x.UserId, x.WeekNumber }).HasDatabaseName("idx_duel_vote_user_week");
        vote.HasIndex(x => new { x.WeekNumber, x.RestaurantId }).HasDatabaseName("idx_duel_vote_week_restaurant");
        vote.HasIndex(x => x.RestaurantId).HasDatabaseName("idx_duel_vote_restaurant");
        vote.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_duel_vote_user");
        vote.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).HasConstraintName("fk_duel_vote_restaurant");

        var badge = model.Entity<Badge>();
        badge.Property(x => x.Name).HasMaxLength(100);
        badge.Property(x => x.IconName).HasMaxLength(100);
        badge.Property(x => x.Description).HasColumnType("text");
        badge.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_badge_name");
        var ub = model.Entity<UserBadge>();
        ub.HasIndex(x => new { x.UserId, x.BadgeId }).IsUnique().HasDatabaseName("uq_user_badge_user_badge");
        ub.HasIndex(x => x.BadgeId).HasDatabaseName("idx_user_badge_badge");
        ub.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_user_badge_user");
        ub.HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeId).HasConstraintName("fk_user_badge_badge");

        // Önceki SQL şemasındaki tekil tablo adlarını ve snake_case sütunlarını korur.
        foreach (var entity in model.Model.GetEntityTypes())
        {
            entity.SetTableName(SnakeCase(entity.ClrType.Name));
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(SnakeCase(property.Name));
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetColumnType("datetime");
                    property.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }
            foreach (var foreignKey in entity.GetForeignKeys())
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
        }
    }

    private static string SnakeCase(string name) =>
        Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}
