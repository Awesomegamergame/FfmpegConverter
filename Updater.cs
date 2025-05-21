using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using FfmpegConverter.Encoders;

internal static class Updater
{
    public static bool CheckAndPromptUpdate(EncoderConfig config)
    {
        string apiUrl = "https://api.github.com/repos/Awesomegamergame/FfmpegConverter/releases/latest";
        Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        string currentVersionTag = "v" + currentVersion.ToString(3); // e.g. v1.1.0

        var request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.UserAgent = "request";
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            string json = reader.ReadToEnd();
            string tag = ExtractJsonValue(json, "tag_name");
            if (string.IsNullOrEmpty(tag) || tag == currentVersionTag)
                return false;

            Version latestVersion = null;
            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                Version.TryParse(tag.Substring(1), out latestVersion);

            if (latestVersion == null || latestVersion <= currentVersion)
                return false;

            // Check if user chose to skip this version
            if (!string.IsNullOrEmpty(config.SkippedVersion) && config.SkippedVersion == tag)
                return false;

            Console.WriteLine($"A new version is available: {tag} (current: {currentVersionTag})");
            Console.Write("Update now? (y = yes, n = no, i = ignore this version): ");
            string input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input == "y")
            {
                string downloadUrl = ExtractAssetUrl(json, "FfmpegConverter.exe");
                if (downloadUrl != null)
                {
                    string tempFile = Path.Combine(Path.GetTempPath(), "FfmpegConverter_update.exe");
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(downloadUrl, tempFile);
                    }
                    Console.WriteLine("Launching updater...");
                    System.Diagnostics.Process.Start(tempFile);
                    Environment.Exit(0);
                }
            }
            else if (input == "i")
            {
                config.SkippedVersion = tag;
                config.Save();
                Console.WriteLine($"You will not be prompted again for version {tag}.");
            }
            // else (n): do nothing, prompt again next time
            return true;
        }
    }

    private static string ExtractJsonValue(string json, string key)
    {
        int idx = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
        if (idx == -1) return null;
        int start = json.IndexOf(':', idx) + 1;
        int quote1 = json.IndexOf('"', start) + 1;
        int quote2 = json.IndexOf('"', quote1);
        return json.Substring(quote1, quote2 - quote1);
    }

    private static string ExtractAssetUrl(string json, string filename)
    {
        int idx = json.IndexOf("\"name\"", StringComparison.OrdinalIgnoreCase);
        while (idx != -1)
        {
            int quote1 = json.IndexOf('"', idx + 6) + 1;
            int quote2 = json.IndexOf('"', quote1);
            string name = json.Substring(quote1, quote2 - quote1);
            if (name.Equals(filename, StringComparison.OrdinalIgnoreCase))
            {
                int urlIdx = json.IndexOf("\"browser_download_url\"", quote2, StringComparison.OrdinalIgnoreCase);
                if (urlIdx != -1)
                {
                    int urlQuote1 = json.IndexOf('"', urlIdx + 23) + 1;
                    int urlQuote2 = json.IndexOf('"', urlQuote1);
                    return json.Substring(urlQuote1, urlQuote2 - urlQuote1);
                }
            }
            idx = json.IndexOf("\"name\"", quote2, StringComparison.OrdinalIgnoreCase);
        }
        return null;
    }
}