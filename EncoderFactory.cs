using System;
using System.Collections.Generic;
using FfmpegConverter.Encoders;

namespace FfmpegConverter
{
    internal static class EncoderFactory
    {
        private delegate string BuildArgumentsDelegate(string input, string output, EncoderConfig config, string codec);

        private static readonly Dictionary<string, BuildArgumentsDelegate> EncoderArgumentBuilders =
            new Dictionary<string, BuildArgumentsDelegate>(StringComparer.OrdinalIgnoreCase)
        {
            ["nvidia"] = (input, output, config, codec) =>
                new NvidiaEncoder().BuildArguments(input, output, config.Nvidia, codec),
            ["intelqsv"] = (input, output, config, codec) =>
                new IntelQsvEncoder().BuildArguments(input, output, config.IntelQsv, codec)
        };

        public static string GetArguments(string hardware, string inputFile, string outputFile, EncoderConfig config, string codecName)
        {
            if (EncoderArgumentBuilders.TryGetValue(hardware, out var builder))
            {
                return builder(inputFile, outputFile, config, codecName);
            }
            throw new NotSupportedException("Unknown encoder type");
        }
    }
}