using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace YemekApp.API.Infrastructure;

// Bütün endpoint'lerin beklenmeyen hatalarını ortak yakalar; SQL ve şifre istemciye dönmez.
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var mysql = exception.GetBaseException() as MySqlException;
        var (status, title) = mysql?.Number switch
        {
            1062 => (409, "Bu kayıt zaten mevcut."),
            1451 or 1452 => (409, "İlişkili kayıt bulunamadı veya kayıt başka bir işlemde değişti."),
            _ when mysql is not null => (503, "Veritabanına erişilemiyor. Yerel bağlantı ve tablo kurulumunu kontrol ediniz."),
            _ => (500, "İşlem sırasında beklenmeyen bir hata oluştu.")
        };
        logger.LogError("API hatası: {Type}, MySQL kodu: {Code}, İz: {TraceId}", exception.GetType().Name, mysql?.Number, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken: ct);
        return true;
    }
}
