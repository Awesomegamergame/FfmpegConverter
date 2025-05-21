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

            return $" {hwaccel}-i \"{inputFile}\" -map 0 -c:v av1_qsv -preset 7 -vf scale_qsv=format=p010le -global_quality {options.CqValue} -c:a copy -c:s copy \"{outputFile}\"";
        }
    }
}