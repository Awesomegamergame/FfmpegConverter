using System;
using FfmpegConverter.Ffmpeg;

namespace FfmpegConverter.Encoders
{
    internal class IntelEncoder : IFfmpegHardwareEncoder<IntelEncoderOptions>
    {
        public string Name => "Intel";

        public string BuildArguments(string inputFile, string outputFile, IntelEncoderOptions options, string codecName)
        {
            string decoder = null;
            switch (codecName.ToLowerInvariant())
            {
                case "h264": decoder = "h264_qsv "; break;
                case "hevc":
                case "h265": decoder = "hevc_qsv "; break;
                case "mpeg2video": decoder = "mpeg2_qsv "; break;
                //case "vc1": decoder = "vc1_qsv "; break; // VC1 is not supported by Intel Arc GPUs
                //case "vp8": decoder = "vp8_qsv "; break; // VP8 is not supported by Intel Arc GPUs
                case "vp9": decoder = "vp9_qsv "; break;
                //case "vvc": decoder = "vvc_qsv "; break; // VVC is not supported by Intel Arc GPUs
                case "av1": decoder = "av1_qsv "; break;
            }

            string hwaccel = "";
            if (!options.ForceCpuDecode && decoder != null)
            {
                hwaccel += "-hwaccel qsv ";
                hwaccel += $"-c:v {decoder} ";
            }
            else
            {
                Console.WriteLine("CPU Decoding Video");
                hwaccel += $"-threads {FfmpegConverter.Utils.GetPhysicalCoreCount()} ";
            }

            hwaccel += "-hwaccel_output_format qsv ";

            string tenBitDepthArgs = options.EnableTenBit ? "-vf scale_qsv=format=p010le" : "";
            string qualityArgs;
            if (options.QualityMode == IntelQualityMode.ICQ)
            {
                qualityArgs = $"-global_quality {options.IcqValue} -look_ahead 1";
            }
            else // CQP
            {
                qualityArgs = $"-q:v {options.CqpValue}";
            }

            return $" {hwaccel}-i \"{inputFile}\" -c:v av1_qsv -async_depth 16 -preset 7 {tenBitDepthArgs} {qualityArgs} -c:a copy -c:s copy \"{outputFile}\"";
        }
    }
}