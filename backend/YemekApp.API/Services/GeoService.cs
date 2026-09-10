namespace YemekApp.API.Services;

public static class GeoService
{
    public const double EarthRadius = 6371000;
    // Haversine mesafesi metre cinsindendir; kayan nokta taşması sınırlandırılır.
    public static double Distance(double lat1, double lng1, double lat2, double lng2)
    {
        static double Rad(double value) => value * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Rad(lat2 - lat1) / 2), 2)
            + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(Rad(lng2 - lng1) / 2), 2);
        return 2 * EarthRadius * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
