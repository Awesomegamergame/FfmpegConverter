using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FfmpegConverter.Encoders;

namespace FfmpegConverter.Ffmpeg
{
    internal static class FfmpegProcessRunner
    {
        private static Process currentFfmpegProcess;
        private static int lastProgressLength = 0;

        public static readonly string[] VideoExtensions = {
            ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm",
            ".asf", ".m4p", ".m4v", ".mpg", ".mp2", ".mpeg", ".mpe",
            ".mpv", ".m2v", ".3gp", ".m2ts", ".ts", ".mts", "3g2"
        };

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

        public static void RegisterShutdownHandlers()
        {
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

        public static bool ConvertWithFfmpeg(string inputFile, string hardware, EncoderConfig config)
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

            string randomStr = Utils.GetRandomString(4);
            string baseName = $"{Path.GetFileNameWithoutExtension(inputFile)}-ffmpeg";
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), baseName + $"-{randomStr}.mkv");

            string arguments = EncoderFactory.GetArguments(hardware, inputFile, outputFile, config, codecName);

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

            var errorOutput = new System.Text.StringBuilder();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null) return;
                errorOutput.AppendLine(e.Data);

                if (e.Data.Contains("frame=") && e.Data.Contains("fps=") && e.Data.Contains("time="))
                {
                    string line = e.Data;
                    string frame = Utils.ExtractValue(line, "frame=");
                    string fps = Utils.ExtractValue(line, "fps=");
                    string q = Utils.ExtractValue(line, "q=");
                    string size = Utils.ExtractValue(line, "size=");
                    string time = Utils.ExtractValue(line, "time=");
                    string bitrate = Utils.ExtractValue(line, "bitrate=");
                    string speed = Utils.ExtractValue(line, "speed=");
                    string elapsed = (DateTime.Now - startTime).ToString(@"h\:mm\:ss\.ff");

                    string fileLabel = $"[{Path.GetFileName(inputFile)}]";
                    string progress = $"{fileLabel} frame={frame} fps={fps} q={q} size={size} time={time} bitrate={bitrate} speed={speed} elapsed={elapsed}";

                    int width = Console.WindowWidth - 1;
                    if (progress.Length > width)
                        progress = progress.Substring(0, width);

                    // Always pad to the full width to clear any leftover characters
                    string padded = progress.PadRight(width);

                    Console.Write($"\r{padded}");

                    // Track the last width used, not the progress length
                    lastProgressLength = width;
                }
            };

            currentFfmpegProcess = process;

            Console.WriteLine($"Converting: {inputFile} \n");
            Console.WriteLine($"ffmpeg {arguments} \n");

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();

            currentFfmpegProcess = null;

            Console.WriteLine();

            if (process.ExitCode == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Success: {outputFile} \n");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error converting {inputFile}");
                Console.ResetColor();
                // Write error output and command to a log file
                var logFile = outputFile + ".log";
                var logContent = new System.Text.StringBuilder();
                logContent.AppendLine("ffmpeg command used:");
                logContent.AppendLine($"ffmpeg {arguments}");
                logContent.AppendLine();
                logContent.AppendLine("ffmpeg output:");
                logContent.AppendLine(errorOutput.ToString());
                File.WriteAllText(logFile, logContent.ToString());
                Console.WriteLine($"ffmpeg output written to: {logFile} \n");
                return false;
            }
        }
    }
}