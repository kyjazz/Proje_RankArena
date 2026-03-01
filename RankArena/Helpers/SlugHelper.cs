using System.Text;
using System.Text.RegularExpressions;

namespace RankArena.Helpers;

public static class SlugHelper
{
    public static string ToSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        text = text.Trim().ToLowerInvariant();

        // Türkçe karakterleri sadeleştir
        text = text
            .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
            .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u");

        // Boşluk ve özel karakterleri -
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        text = text.Replace(" ", "-");

        // Çoklu '-' temizle
        text = Regex.Replace(text, @"-+", "-");

        return text;
    }
}
