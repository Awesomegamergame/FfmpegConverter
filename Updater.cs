using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using FfmpegConverter.Encoders;
using System.Linq;

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
            if (!string.IsNullOrEmpty(config.Program.SkippedVersion) && config.Program.SkippedVersion == tag)
                return false;

            Console.WriteLine($"A new version is available: {tag} (current: {currentVersionTag})");
            Console.Write("Update now? (y = yes, n = no, i = ignore this version): ");
            string input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input == "y")
            {
                string downloadUrl = ExtractAssetUrl(json, "FfmpegConverter.exe");
                if (downloadUrl != null)
                {
                    string exePath = Assembly.GetExecutingAssembly().Location;
                    string exeDir = Path.GetDirectoryName(exePath);
                    string exeName = Path.GetFileName(exePath);
                    string oldExe = Path.Combine(exeDir, "FfmpegConverter.old.exe");
                    string newExe = Path.Combine(exeDir, "FfmpegConverter.exe");

                    // Rename current exe to .old
                    File.Move(exePath, oldExe);

                    // Download new exe to the original name
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(downloadUrl, newExe);
                    }

                    Console.WriteLine("Launching updated version...");
                    Console.ReadKey();
                    System.Diagnostics.Process.Start(newExe, "--cleanup-old");
                    Environment.Exit(0);
                }
            }
            else if (input == "i")
            {
                config.Program.SkippedVersion = tag;
                config.Save();
                Console.WriteLine($"You will not be prompted again for version {tag}.");
            }
            // else (n): do nothing, prompt again next time
            return true;
        }
    }

    public static bool HandlePostUpdate(string[] args)
    {
        if (args != null && !args.Contains("--cleanup-old"))
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string oldExe = Path.Combine(exeDir, "FfmpegConverter.old.exe");
            try
            {
                if (File.Exists(oldExe))
                    File.Delete(oldExe);
            }
            catch
            {
                Console.WriteLine("Warning: Could not delete old version.");
            }

            //Rename config.json to config.old.json
            string configFile = Path.Combine(exeDir, EncoderConfig.ConfigFileName);
            string oldConfigFile = Path.Combine(exeDir, "config.old.json");
            try
            {
                if (File.Exists(configFile))
                    File.Move(configFile, oldConfigFile);
            }
            catch
            {
                Console.WriteLine("Warning: Could not rename config file.");
                Console.WriteLine("It's reccomended to remove your config and allow the program to create a new one.");
            }

            Console.WriteLine("Update complete.");
            // Continue to normal startup below
        }
        return false; // continue as normal
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