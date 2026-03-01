using Microsoft.AspNetCore.Http;

namespace RankArena.Helpers;

public static class SessionKeyHelper
{
    private const string CookieName = "ra_session";

    public static string GetOrCreate(HttpContext http)
    {
        if (http.Request.Cookies.TryGetValue(CookieName, out var key) &&
            !string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        var newKey = Guid.NewGuid().ToString("N");

        http.Response.Cookies.Append(CookieName, newKey, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(60)
        });

        return newKey;
    }
}
