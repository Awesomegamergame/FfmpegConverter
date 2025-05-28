using System;
using System.IO;
using System.Linq;
using FfmpegConverter.Ffmpeg;
using FfmpegConverter.Encoders;

namespace FfmpegConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (Updater.HandlePostUpdate(args))
                return;

            FfmpegProcessRunner.RegisterShutdownHandlers();

            bool configCreated;
            var config = EncoderConfig.LoadOrCreate(out configCreated);

            // Reset skip if updated
            config.ResetSkippedVersionIfOutdated();

            // Check for update before converting
            Updater.CheckAndPromptUpdate(config);

            if (configCreated)
            {
                Console.WriteLine("Configuration file created: config.json");
                Console.WriteLine("You can edit the file for advanced settings if needed.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                Console.WriteLine();
            }

            // Prompt for GPU vendor
            string gpuVendor = null;
            while (gpuVendor == null)
            {
                Console.Write("Select GPU vendor ([n]vidia / [i]ntel): ");
                var input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (input == "n" || input == "nvidia")
                    gpuVendor = "nvidia";
                else if (input == "i" || input == "intel")
                    gpuVendor = "intel";
                else
                    Console.WriteLine("Invalid input. Please enter 'n' for Nvidia or 'i' for Intel.");
            }
            Console.WriteLine();

            string[] filesToConvert;
            if (args != null && args.Length > 0 && File.Exists(args[0]))
            {
                filesToConvert = args.Where(File.Exists).ToArray();
            }
            else
            {
                string currentDir = Directory.GetCurrentDirectory();
                filesToConvert = Directory.GetFiles(currentDir)
                    .Where(f => FfmpegProcessRunner.VideoExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (filesToConvert.Length == 0)
            {
                Console.WriteLine("No video files found to convert.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            int errorCount = 0;
            foreach (var file in filesToConvert)
            {
                bool success = FfmpegProcessRunner.ConvertWithFfmpeg(file, gpuVendor, config);
                if (!success)
                    errorCount++;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Conversion complete.");
            if (errorCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{errorCount} file(s) failed to convert.");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press any key to exit.");
            Console.ResetColor();
            Console.ReadKey();
        }
    }
}