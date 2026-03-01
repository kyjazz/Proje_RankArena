using System.Text.RegularExpressions;

namespace RankArena.Helpers;

public static class YouTubeHelper
{
    // Dönüş: videoId veya null
    public static string? ExtractVideoId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Kullanıcı sadece ID girdiyse (genelde 11 karakter)
        // YouTube video id formatı: [A-Za-z0-9_-]{11}
        if (Regex.IsMatch(input, @"^[A-Za-z0-9_-]{11}$"))
            return input;

        // https://www.youtube.com/watch?v=VIDEOID
        var m1 = Regex.Match(input, @"(?:v=)([A-Za-z0-9_-]{11})");
        if (m1.Success) return m1.Groups[1].Value;

        // https://youtu.be/VIDEOID
        var m2 = Regex.Match(input, @"youtu\.be\/([A-Za-z0-9_-]{11})");
        if (m2.Success) return m2.Groups[1].Value;

        // https://www.youtube.com/embed/VIDEOID
        var m3 = Regex.Match(input, @"embed\/([A-Za-z0-9_-]{11})");
        if (m3.Success) return m3.Groups[1].Value;

        // Shorts: https://www.youtube.com/shorts/VIDEOID
        var m4 = Regex.Match(input, @"shorts\/([A-Za-z0-9_-]{11})");
        if (m4.Success) return m4.Groups[1].Value;

        // Bulamazsak null
        return null;
    }
}
