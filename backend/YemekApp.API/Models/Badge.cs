namespace YemekApp.API.Models;

public class Badge
{
    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public string? IconName { get; set; }
    public string? Description { get; set; }
}
