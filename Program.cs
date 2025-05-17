using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FfmpegConverter
{
    internal class Program
    {
        private static Process currentFfmpegProcess; // Track the running ffmpeg process

        // List of common video file extensions
        private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm" };

        static void Main(string[] args)
        {
            // Handle console close (Ctrl+C, window close, etc.)
            Console.CancelKeyPress += (sender, e) =>
            {
                KillFfmpegIfRunning();
                Environment.Exit(0);
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                KillFfmpegIfRunning();
            };

            string[] filesToConvert;

            if (args != null && args.Length > 0 && File.Exists(args[0]))
            {
                // File(s) dragged and dropped onto the executable
                filesToConvert = args.Where(File.Exists).ToArray();
            }
            else
            {
                // No file dropped, convert all video files in the current directory
                string currentDir = Directory.GetCurrentDirectory();
                filesToConvert = Directory.GetFiles(currentDir)
                    .Where(f => VideoExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (filesToConvert.Length == 0)
            {
                Console.WriteLine("No video files found to convert.");
                return;
            }

            foreach (var file in filesToConvert)
            {
                ConvertWithFfmpeg(file);
            }

            Console.WriteLine("Conversion complete. Press any key to exit.");
            Console.ReadKey();
        }

        private static void KillFfmpegIfRunning()
        {
            try
            {
                if (currentFfmpegProcess != null && !currentFfmpegProcess.HasExited)
                {
                    currentFfmpegProcess.Kill();
                    currentFfmpegProcess.WaitForExit();
                }
            }
            catch { /* Ignore exceptions on exit */ }
        }

        private static void ConvertWithFfmpeg(string inputFile)
        {
            // Defaults
            int cqValue = 30;
            bool enableSwDecoding = false;

            // Look for any .txt file in the current directory
            string currentDir = Path.GetDirectoryName(inputFile);
            string txtFile = Directory.GetFiles(currentDir, "*.txt").FirstOrDefault();

            if (txtFile != null)
            {
                try
                {
                    var lines = File.ReadAllLines(txtFile);
                    if (lines.Length > 0 && int.TryParse(lines[0], out int parsedCq))
                        cqValue = parsedCq;
                    if (lines.Length > 1 && bool.TryParse(lines[1], out bool parsedSw))
                        enableSwDecoding = parsedSw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading config from {txtFile}: {ex.Message}");
                    Console.WriteLine("Using default CQ and decoding settings.");
                }
            }

            // Generate 4 random uppercase alphanumeric characters
            string randomStr = GetRandomString(4);

            // Output file: original name + -ffmpeg-(cq)-(random).mkv
            string outputFile = Path.Combine(
                Path.GetDirectoryName(inputFile),
                $"{Path.GetFileNameWithoutExtension(inputFile)}-ffmpeg-{cqValue}-{randomStr}.mkv"
            );

            // Build ffmpeg arguments
            // Always include -hwaccel_output_format cuda, only add -hwaccel nvdec if not using SW decoding
            string hwaccel = enableSwDecoding ? "-hwaccel_output_format cuda " : "-hwaccel nvdec -hwaccel_output_format cuda ";
            string swthreads = enableSwDecoding ? "-threads 0 " : "";
            string arguments = $" {hwaccel}{swthreads}-i \"{inputFile}\" -map 0 -c:v av1_nvenc -highbitdepth true -split_encode_mode forced -preset p1 -cq {cqValue} -b:v 0 -c:a copy -c:s copy \"{outputFile}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            currentFfmpegProcess = process; // Track the process

            Console.WriteLine($"Converting: {inputFile}");
            Console.WriteLine($"  CQ: {cqValue}, SW Decoding: {enableSwDecoding}");

            string lastStatLine = null;
            DateTime lastPrint = DateTime.MinValue;

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null) return;
                if (e.Data.Contains("time=") || e.Data.Contains("fps="))
                {
                    lastStatLine = $"[{Path.GetFileName(inputFile)}] {e.Data}";
                    if ((DateTime.Now - lastPrint).TotalSeconds >= 2)
                    {
                        Console.WriteLine(lastStatLine);
                        lastPrint = DateTime.Now;
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();

            currentFfmpegProcess = null; // Clear after done

            // Print the last stat line if it wasn't printed in the last 5 seconds
            if (lastStatLine != null && (DateTime.Now - lastPrint).TotalSeconds > 1)
            {
                Console.WriteLine(lastStatLine);
            }

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"Success: {outputFile}");
            }
            else
            {
                Console.WriteLine($"Error converting {inputFile}");
            }
        }

        // Helper method to generate a random alphanumeric string
        private static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}