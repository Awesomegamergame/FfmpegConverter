using System;
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
    }
}