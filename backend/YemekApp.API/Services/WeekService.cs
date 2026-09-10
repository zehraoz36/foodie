using System.Globalization;

namespace YemekApp.API.Services;

public static class WeekService
{
    public static uint Current(DateTime utcNow)
    {
        var turkey = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
        return (uint)(ISOWeek.GetYear(turkey) * 100 + ISOWeek.GetWeekOfYear(turkey));
    }
}
