using FfmpegConverter.Ffmpeg;

namespace FfmpegConverter.Encoders
{
    internal class IntelEncoder : IFfmpegHardwareEncoder<IntelEncoderOptions>
    {
        public string Name => "Intel";

        public string BuildArguments(string inputFile, string outputFile, IntelEncoderOptions options, string codecName)
        {
            string decoder = "";
            switch (codecName.ToLowerInvariant())
            {
                case "h264": decoder = "h264_qsv "; break;
                case "hevc":
                case "h265": decoder = "hevc_qsv "; break;
                case "mpeg2video": decoder = "mpeg2_qsv "; break;
                case "vc1": decoder = "vc1_qsv "; break;
                case "vp8": decoder = "vp8_qsv "; break;
                case "vp9": decoder = "vp9_qsv "; break;
                case "vvc": decoder = "vvc_qsv "; break;
                case "av1": decoder = "av1_qsv "; break;
            }

            string hwaccel = "-hwaccel qsv ";
            if (decoder != null)
                hwaccel += $"-c:v {decoder} ";
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