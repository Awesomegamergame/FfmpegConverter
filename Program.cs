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
        private static readonly string[] VideoExtensions = {
            ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm",
            ".asf", ".m4p", ".m4v", ".mpg", ".mp2", ".mpeg", ".mpe",
            ".mpv", ".m2v", ".3gp", ".m2ts", ".ts", ".mts", "3g2"
        };

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
            bool enableSpatialAq = false;
            int aqStrength = 8;
            bool enableSwDecoding = false;

            // Look for any .txt file in the current directory
            string currentDir = Path.GetDirectoryName(inputFile);
            string txtFile = Directory.GetFiles(currentDir, "*.txt").FirstOrDefault();

            if (txtFile != null)
            {
                try
                {
                    var lines = File.ReadAllLines(txtFile);

                    // First line: CQ value
                    if (lines.Length > 0 && int.TryParse(lines[0], out int parsedCq))
                        cqValue = parsedCq;

                    // Second line: spatial-aq and aq-strength
                    if (lines.Length > 1)
                    {
                        var parts = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0 && bool.TryParse(parts[0], out bool parsedSpatialAq))
                            enableSpatialAq = parsedSpatialAq;
                        if (enableSpatialAq && parts.Length > 1 && int.TryParse(parts[1], out int parsedAqStrength) && parsedAqStrength >= 1 && parsedAqStrength <= 15)
                            aqStrength = parsedAqStrength;
                    }

                    // Third line: software decoding
                    if (lines.Length > 2 && bool.TryParse(lines[2], out bool parsedSw))
                        enableSwDecoding = parsedSw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading config from {txtFile}: {ex.Message}");
                    Console.WriteLine("Using default CQ and decoding settings.");
                }
            }

            // Get video codec using ffprobe
            string codecName = "unknown";
            try
            {
                var probeProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffprobe",
                        Arguments = $"-v error -select_streams v:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                probeProcess.Start();
                string output = probeProcess.StandardOutput.ReadToEnd().Trim();
                probeProcess.WaitForExit();
                if (!string.IsNullOrWhiteSpace(output))
                    codecName = output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ffprobe error: {ex.Message}]");
            }

            // Map ffprobe codec name to cuvid decoder
            string cuvidDecoder = null;
            if (!enableSwDecoding)
            {
                switch (codecName.ToLowerInvariant())
                {
                    case "h264":
                        cuvidDecoder = "h264_cuvid";
                        break;
                    case "hevc":
                    case "h265":
                        cuvidDecoder = "hevc_cuvid";
                        break;
                    case "mpeg1video":
                        cuvidDecoder = "mpeg1_cuvid";
                        break;
                    case "mpeg2video":
                        cuvidDecoder = "mpeg2_cuvid";
                        break;
                    case "vc1":
                        cuvidDecoder = "vc1_cuvid";
                        break;
                    case "vp8":
                        cuvidDecoder = "vp8_cuvid";
                        break;
                    case "vp9":
                        cuvidDecoder = "vp9_cuvid";
                        break;
                    case "av1":
                        cuvidDecoder = "av1_cuvid";
                        break;
                }
            }

            // Generate 4 random uppercase alphanumeric characters
            string randomStr = GetRandomString(4);

            // Output file: original name + -ffmpeg-(cq)[-aq-##]-(random).mkv
            string baseName = $"{Path.GetFileNameWithoutExtension(inputFile)}-ffmpeg-{cqValue}";
            if (enableSpatialAq)
                baseName += $"-aq-{aqStrength}";
            baseName += $"-{randomStr}.mkv";
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), baseName);

            // Build ffmpeg arguments
            string hwaccel = enableSwDecoding ? "-hwaccel_output_format cuda " : "-hwaccel nvdec ";
            if (!enableSwDecoding && cuvidDecoder != null)
                hwaccel += $"-c:v {cuvidDecoder} ";
            hwaccel += "-hwaccel_output_format cuda ";

            string swthreads = enableSwDecoding ? "-threads 0 " : "";

            string aqArgs = "";
            if (enableSpatialAq)
                aqArgs = $"-spatial-aq 1 -aq-strength {aqStrength} ";

            string arguments = $" {hwaccel}{swthreads}-i \"{inputFile}\" -map 0 -c:v av1_nvenc -highbitdepth true -split_encode_mode forced -preset p1 -cq {cqValue} -b:v 0 {aqArgs}-c:a copy -c:s copy \"{outputFile}\"";

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
            Console.WriteLine($"  CQ: {cqValue}, SW Decoding: {enableSwDecoding}, Spatial AQ: {enableSpatialAq}, AQ Strength: {(enableSpatialAq ? aqStrength.ToString() : "N/A")}");
            Console.WriteLine($"Input video codec: {codecName}");

            // Print the full ffmpeg command
            Console.WriteLine($"ffmpeg {arguments}");

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