using System.ComponentModel.DataAnnotations;

namespace YemekApp.API.Dtos;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir."), StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3–50 karakter olmalıdır.")]
    public string Username { get; set; } = "";
    [Required(ErrorMessage = "E-posta gereklidir."), EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz."), StringLength(254)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Şifre gereklidir."), StringLength(128, MinimumLength = 8, ErrorMessage = "Şifre 8–128 karakter olmalıdır.")]
    public string Password { get; set; } = "";
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "E-posta gereklidir."), EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz."), StringLength(254)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Şifre gereklidir."), StringLength(128)]
    public string Password { get; set; } = "";
}

public sealed class RestaurantActionRequest
{
    // Kimlik JWT'den alınır. Eski istemci userId gönderirse sahiplik ayrıca doğrulanır.
    public ulong? UserId { get; set; }
    [Range(typeof(ulong), "1", "18446744073709551615", ErrorMessage = "Restoran kimliği pozitif olmalıdır.")]
    public ulong RestaurantId { get; set; }
}

public sealed class SpinRequest
{
    public ulong? UserId { get; set; }
    [Range(typeof(ulong), "1", "18446744073709551615", ErrorMessage = "Sonuç restoranı gereklidir.")]
    public ulong ResultRestaurantId { get; set; }
}

public sealed class NearbyRequest
{
    [Required(ErrorMessage = "Enlem gereklidir."), Range(-90d, 90d, ErrorMessage = "Enlem -90 ile 90 arasında olmalıdır.")]
    public double? Lat { get; set; }
    [Required(ErrorMessage = "Boylam gereklidir."), Range(-180d, 180d, ErrorMessage = "Boylam -180 ile 180 arasında olmalıdır.")]
    public double? Lng { get; set; }
    [Range(1, 100000, ErrorMessage = "Yarıçap 1–100000 metre arasında olmalıdır.")]
    public int Radius { get; set; } = 2000;
    [StringLength(100)]
    public string? Category { get; set; }
    [Range(1, 100000, ErrorMessage = "Sayfa 1–100000 arasında olmalıdır.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "Sayfa boyutu 1–100 arasında olmalıdır.")]
    public int PageSize { get; set; } = 50;
}
