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
            FfmpegProcessRunner.RegisterShutdownHandlers();

            bool configCreated;
            var config = EncoderConfig.LoadOrCreate(out configCreated);

            if (configCreated)
            {
                Console.WriteLine("Configuration file created: encoderconfig.json");
                Console.WriteLine("Please edit the file to choose your GPU vendor (nvidia/intel) and settings, then run the program again.");
                return;
            }

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
                return;
            }

            foreach (var file in filesToConvert)
            {
                FfmpegProcessRunner.ConvertWithFfmpeg(file, config.GpuVendor, config);
            }

            Console.WriteLine("Conversion complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}