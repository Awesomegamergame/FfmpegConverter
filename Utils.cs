using FfmpegConverter.Encoders;
using FfmpegConverter.Ffmpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;

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

        public static int GetPhysicalCoreCount()
        {
            try
            {
                int coreCount = 0;
                using (var searcher = new ManagementObjectSearcher("select NumberOfCores from Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        coreCount += Convert.ToInt32(item["NumberOfCores"]);
                    }
                }
                return coreCount > 0 ? coreCount : Environment.ProcessorCount;
            }
            catch
            {
                // Fallback to logical processor count if WMI fails
                return Environment.ProcessorCount;
            }
        }

        public static string[] FindFiles(string[] args, EncoderConfig config)
        {
            Func<string, bool> isOriginalVideo = f =>
                FfmpegProcessRunner.VideoExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase) &&
                !Path.GetFileName(f).ToLowerInvariant().Contains("ffmpeg");

            if (args != null && args.Length > 0)
            {
                var folderArgs = args.Where(Directory.Exists).ToArray();
                var fileArgs = args.Where(File.Exists).ToArray();

                var files = new List<string>();
                var searchOption = config.Program.SearchSubdirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                foreach (var folder in folderArgs)
                {
                    files.AddRange(
                        Directory.GetFiles(folder, "*.*", searchOption)
                            .Where(isOriginalVideo)
                    );
                }

                files.AddRange(fileArgs.Where(isOriginalVideo));

                return files.ToArray();
            }
            else
            {
                string currentDir = Directory.GetCurrentDirectory();
                var searchOption = config.Program.SearchSubdirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                return Directory.GetFiles(currentDir, "*.*", searchOption)
                    .Where(isOriginalVideo)
                    .ToArray();
            }
        }
    }
}