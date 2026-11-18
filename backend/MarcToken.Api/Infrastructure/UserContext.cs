using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MarcToken.Api.Infrastructure;

public static class UserContext
{
    public static Guid GetUserIdOrThrow(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("user_id");

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        }

        if (Guid.TryParse(raw, out var userId))
        {
            return userId;
        }

        // Firebase Auth string UID'lerini deterministik bir Guid'e dönüştürür.
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return new Guid(hash);
    }

    public static string? GetEmail(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");

    public static bool IsAnonymous(ClaimsPrincipal user)
    {
        var firebaseClaim = user.FindFirst("firebase")?.Value;
        if (!string.IsNullOrEmpty(firebaseClaim) && firebaseClaim.Contains("\"sign_in_provider\":\"anonymous\""))
        {
            return true;
        }
        return user.FindFirstValue("sign_in_provider") == "anonymous";
    }
}
