# YemekApp Backend

.NET 8 Web API, EF Core 8, Pomelo MySQL ve JWT kullanır. Frontend bağımsızdır.

## Dosya yapısı

- `YemekApp.API/Models/`: mevcut 10 MySQL tablosunun modelleri.
- `YemekApp.API/Data/AppDbContext.cs`: sütun adları, veri tipleri, indeksler ve ilişkiler.
- `YemekApp.API/Controllers/`: restoran, favori, çark, düello ve kimlik doğrulama uçları.
- `YemekApp.API/Dtos/`: istek doğrulamaları ve güvenli yanıt modelleri.
- `YemekApp.API/Services/`: JWT üretimi, Haversine ve Türkiye saatine göre ISO hafta hesabı.
- `YemekApp.API/Infrastructure/`: ortak hata yakalama.
- `YemekApp.API.Tests/`: ayrı geçici MySQL veritabanında uçtan uca testler.

## Yerel çalıştırma

`backend/` klasöründen:

```bash
dotnet restore YemekApp.sln
dotnet run --project YemekApp.API --launch-profile http
```

Swagger: http://localhost:5080/swagger

MySQL `127.0.0.1:3306` üzerinde çalışmalı, önceki SQL scriptindeki `yemek_app` ve 10 tablo mevcut olmalıdır. API başlangıçta tablo oluşturmaz, migration çalıştırmaz veya örnek veri eklemez. Mevcut kurulumda MySQL 9.4.0 ile de test edilmiştir; sorgular MySQL 8 uyumluluğuyla üretilir.

`YemekApp.API/appsettings.json` şifresiz şablondur. Bu bilgisayarda gerçek bağlantı ve rastgele oluşturulan JWT anahtarı `appsettings.Local.json` içinde tutulur. Bu dosya Git dışında ve publish çıktısının dışındadır. Yalnızca Development ortamında yüklenir.

Yeni bir bilgisayarda `YemekApp.API/appsettings.Local.json` oluştur:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Port=3306;Database=yemek_app;Uid=root;Pwd=BURAYA_ŞIFRE;"
  },
  "Jwt": {
    "Key": "BURAYA_EN_AZ_32_BAYT_RASTGELE_ANAHTAR"
  }
}
```

Anahtarı `openssl rand -base64 48` ile üret. Production ortamında `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` ortam değişkenlerini kullan. JWT anahtarı eksikse uygulama açıklayıcı hata ile başlamaz. Token süresi varsayılan 60 dakikadır. CORS sadece `http://localhost:8100` için açıktır.

## API kullanımı

Kayıt: `POST /api/auth/register`

```json
{ "username": "kullanici", "email": "kullanici@example.com", "password": "EN_AZ_8_KARAKTER" }
```

Giriş: `POST /api/auth/login`

```json
{ "email": "kullanici@example.com", "password": "EN_AZ_8_KARAKTER" }
```

Her iki yanıt da `token`, `expiresAt`, `userId`, `username` içerir. Şifre veya hash değeri dönmez. Şifre ASP.NET Identity PasswordHasher ile hashlenir. Swagger'da **Authorize** alanına yalnızca token değerini gir. HTTP istemcisinde `Authorization: Bearer TOKEN` başlığı kullan.

| Yöntem | Yol | Açıklama |
| --- | --- | --- |
| GET | `/api/restaurants?lat=41&lng=29&radius=2000&category=soslu-tavuk` | Metre cinsinden yarıçap, en yakından uzağa restoranlar |
| GET | `/api/restaurants/{id}` | Restoran ve kategorileri |
| GET | `/api/restaurants/{id}/menu` | Mevcut olmayan ürünleri de `isAvailable` bilgisiyle döndüren menü |
| GET | `/api/favorites/{userId}` | Yalnızca giriş yapan kişinin favorileri |
| POST | `/api/favorites` | `{ "restaurantId": 1 }` |
| DELETE | `/api/favorites/{id}` | Yalnızca kişinin kendi favorisini siler |
| POST | `/api/spin` | `{ "resultRestaurantId": 1 }`, frontend çark sonucunu kaydeder |
| GET | `/api/duel/weekly` | Türkiye saatine göre güncel ISO haftasının oy sayıları |
| POST | `/api/duel/vote` | `{ "restaurantId": 1 }` |

Favori, çark ve oy yazma işlemleri JWT gerektirir. Kullanıcı kimliği JWT'den alınır; isteğe `userId` eklenirse JWT ile eşleşmesi zorunludur. Restoran okuma ve haftalık sonuçlar herkese açıktır. Silinmiş kullanıcıların tokenları reddedilir.

Konum parametreleri zorunludur. Yarıçap 1–100000 metre, `page` varsayılan 1, `pageSize` varsayılan 50 ve en fazla 100'dür. Mesafe `distanceMeters` alanındadır. Şemada kategori slug sütunu olmadığından `soslu-tavuk`, `Soslu tavuk` adına çevrilir. Diğer kategori adlarında da boşluklar tireyle yazılır; Türkçe karakterler URL kodlamasıyla gönderilebilir.

`week_number`, ISO hafta yılıyla birlikte `YYYYWW` biçiminde saklanır. Yıl sınırında takvim yılı yerine ISO hafta yılı kullanılır. Mevcut şemada haftalık tek oy kısıtı bulunmadığı için her geçerli oy isteği ayrı kayıt oluşturur; haftalık sonuçlar bu kayıtları sayar.

Hatalar: geçersiz giriş 400, giriş gerekli 401, yetkisiz sahiplik 403, bulunamayan kayıt 404, mükerrer favori/kullanıcı 409, giriş/kayıtta IP başına dakikada 20 deneme sınırı 429, veritabanına erişim veya tablo sorunu 503, beklenmeyen hata 500. Ortak exception handler bütün controller'ları kapsar; yanıtlar SQL ve bağlantı şifresini içermez.

## Doğrulama

```bash
dotnet build YemekApp.sln --no-restore -warnaserror
dotnet test YemekApp.sln --no-restore
```

Testler yerel bağlantıyı okur; alternatif olarak `YEMEK_TEST_MYSQL` ortam değişkeni kullanılabilir. Kullanıcının geçici veritabanı oluşturma/silme yetkisi olmalıdır. Her çalıştırmada `yemek_app_test_<rastgele>` oluşturulur ve test bitiminde silinir. Test verileri gerçek `yemek_app` içine yazılmaz. Testler Haversine ve kategori sorgularını gerçek MySQL'de, JWT/sahiplik kontrollerini HTTP üzerinden doğrular.

Paket uyumluluğu: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql
