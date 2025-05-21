using System;
using System.IO;
using System.Linq;
using FfmpegConverter.Encoders;

namespace FfmpegConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FfmpegProcessRunner.RegisterShutdownHandlers();

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
                // Change "nvidia" to "intelqsv" to use Intel QSV
                FfmpegProcessRunner.ConvertWithFfmpeg(file, "nvidia", config);
            }

            Console.WriteLine("Conversion complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}