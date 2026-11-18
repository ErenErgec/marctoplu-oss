using System.Text.Json;

namespace MarcToken.Api.Services;

/// <summary>
/// JWS (header.payload.signature) payload'unu imzayı doğrulamadan okur. Yalnızca içeriği zaten güvenilir
/// bir kanaldan gelen (App Store Server API yanıtı) ya da sadece tetikleyici olarak kullanılan
/// (mağaza bildirimi; karar mağaza API'sine sorularak verilir) veriler için kullanılmalıdır.
/// </summary>
public static class JwsPayload
{
    public static JsonDocument? TryParse(string? jws)
    {
        var parts = jws?.Split('.');
        if (parts is not { Length: 3 })
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(Base64UrlDecode(parts[1]));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    public static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
