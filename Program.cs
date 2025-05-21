using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FfmpegConverter.Encoders;

namespace FfmpegConverter
{
    internal class Program
    {
        private static Process currentFfmpegProcess;
        private static int lastProgressLength = 0;

        // Windows console control handler
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("\n");
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");
            KillFfmpegIfRunning();
            Console.WriteLine("Cleanup complete");
            Environment.Exit(-1);
            return true;
        }

        private static readonly string[] VideoExtensions = {
            ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm",
            ".asf", ".m4p", ".m4v", ".mpg", ".mp2", ".mpeg", ".mpe",
            ".mpv", ".m2v", ".3gp", ".m2ts", ".ts", ".mts", "3g2"
        };

        static void Main(string[] args)
        {
            // Register shutdown handlers
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            Console.CancelKeyPress += (sender, e) =>
            {
                KillFfmpegIfRunning();
                Environment.Exit(0);
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                KillFfmpegIfRunning();
            };

            var config = EncoderConfig.LoadOrCreate();

            string[] filesToConvert;

            if (args != null && args.Length > 0 && File.Exists(args[0]))
            {
                filesToConvert = args.Where(File.Exists).ToArray();
            }
            else
            {
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
                // Change "nvidia" to "intelqsv" to use Intel QSV
                ConvertWithFfmpeg(file, "nvidia", config);
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
            catch { }
        }

        private static void ConvertWithFfmpeg(string inputFile, string hardware, EncoderConfig config)
        {
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
            catch
            {
                Console.WriteLine("[ffprobe error]");
            }

            string randomStr = GetRandomString(4);
            string baseName = $"{Path.GetFileNameWithoutExtension(inputFile)}-ffmpeg";
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), baseName + $"-{randomStr}.mkv");

            string arguments;
            if (hardware.Equals("nvidia", StringComparison.OrdinalIgnoreCase))
            {
                var options = config.Nvidia;
                var encoder = new NvidiaEncoder();
                arguments = encoder.BuildArguments(inputFile, outputFile, options, codecName);
            }
            else if (hardware.Equals("intelqsv", StringComparison.OrdinalIgnoreCase))
            {
                var options = config.IntelQsv;
                var encoder = new IntelQsvEncoder();
                arguments = encoder.BuildArguments(inputFile, outputFile, options, codecName);
            }
            else
            {
                throw new NotSupportedException("Unknown encoder type");
            }

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

            var startTime = DateTime.Now;

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null) return;
                if (e.Data.Contains("frame=") && e.Data.Contains("fps=") && e.Data.Contains("time="))
                {
                    string line = e.Data;
                    string frame = ExtractValue(line, "frame=");
                    string fps = ExtractValue(line, "fps=");
                    string q = ExtractValue(line, "q=");
                    string size = ExtractValue(line, "size=");
                    string time = ExtractValue(line, "time=");
                    string bitrate = ExtractValue(line, "bitrate=");
                    string speed = ExtractValue(line, "speed=");
                    string elapsed = (DateTime.Now - startTime).ToString(@"h\:mm\:ss\.ff");

                    string fileLabel = $"[{Path.GetFileName(inputFile)}]";
                    string progress = $"{fileLabel} frame={frame} fps={fps} q={q} size={size} time={time} bitrate={bitrate} speed={speed} elapsed={elapsed}";

                    int width = Console.WindowWidth - 1;
                    if (progress.Length > width)
                        progress = progress.Substring(0, width);

                    // Pad with spaces to clear previous content, using the max of previous and current width
                    int padLength = Math.Max(lastProgressLength, progress.Length);
                    string padded = progress.PadRight(padLength);

                    Console.Write($"\r{padded}");

                    lastProgressLength = progress.Length;
                }
            };

            currentFfmpegProcess = process;

            Console.WriteLine($"Converting: {inputFile}");
            Console.WriteLine($"ffmpeg {arguments}");

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();

            currentFfmpegProcess = null;

            Console.WriteLine(); // Move to next line after progress

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"Success: {outputFile}");
            }
            else
            {
                Console.WriteLine($"Error converting {inputFile}");
            }
        }

        // Helper to extract value after a key (e.g., "frame=")
        private static string ExtractValue(string line, string key)
        {
            int idx = line.IndexOf(key, StringComparison.Ordinal);
            if (idx == -1) return "";
            idx += key.Length;
            int end = line.IndexOf(' ', idx);
            if (end == -1) end = line.Length;
            return line.Substring(idx, end - idx).Trim();
        }

        private static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}