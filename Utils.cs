using System;
using System.Linq;

namespace FfmpegConverter
{
    internal static class Utils
    {
        public static string ExtractValue(string line, string key)
        {
            int idx = line.IndexOf(key, StringComparison.Ordinal);
            if (idx == -1) return "";
            idx += key.Length;
            int end = line.IndexOf(' ', idx);
            if (end == -1) end = line.Length;
            return line.Substring(idx, end - idx).Trim();
        }

        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}