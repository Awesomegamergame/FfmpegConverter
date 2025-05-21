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
                case "h264": decoder = "-c:v h264_qsv "; break;
                case "hevc":
                case "h265": decoder = "-c:v hevc_qsv "; break;
                case "mpeg2video": decoder = "-c:v mpeg2_qsv "; break;
            }

            string hwaccel = "-hwaccel qsv ";

            return $" {hwaccel}{decoder}-i \"{inputFile}\" -map 0 -c:v av1_qsv -preset 7 -global_quality {options.CqValue} -b:v 0 -c:a copy -c:s copy \"{outputFile}\"";
        }
    }
}