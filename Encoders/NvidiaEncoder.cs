using FfmpegConverter.Ffmpeg;

namespace FfmpegConverter.Encoders
{
    internal class NvidiaEncoder : IFfmpegHardwareEncoder<NvidiaEncoderOptions>
    {
        public string Name => "NVIDIA";

        public string BuildArguments(string inputFile, string outputFile, NvidiaEncoderOptions options, string codecName)
        {
            string cuvidDecoder = null;
            switch (codecName.ToLowerInvariant())
            {
                case "h264": cuvidDecoder = "h264_cuvid"; break;
                case "hevc":
                case "h265": cuvidDecoder = "hevc_cuvid"; break;
                case "mpeg1video": cuvidDecoder = "mpeg1_cuvid"; break;
                case "mpeg2video": cuvidDecoder = "mpeg2_cuvid"; break;
                case "mpeg4": cuvidDecoder = "mpeg4_cuvid"; break;
                case "vc1": cuvidDecoder = "vc1_cuvid"; break;
                case "vp8": cuvidDecoder = "vp8_cuvid"; break;
                case "vp9": cuvidDecoder = "vp9_cuvid"; break;
                case "av1": cuvidDecoder = "av1_cuvid"; break;
            }

            string hwaccel = "-hwaccel nvdec ";
            if (cuvidDecoder != null)
                hwaccel += $"-c:v {cuvidDecoder} ";
            hwaccel += "-hwaccel_output_format cuda ";

            string tenBitDepthArgs = options.EnableTenBit ? "-highbitdepth true" : "";
            string aqSpatialArgs = options.EnableSpatialAq ? $"-spatial-aq 1 -aq-strength {options.AqStrength}" : "";
            string aqTemporalArgs = options.EnableTemporalAq ? "-temporal-aq 1" : "";

            return $" {hwaccel}-i \"{inputFile}\" -c:v av1_nvenc {tenBitDepthArgs} -split_encode_mode forced -preset p1 -cq {options.CqValue} -b:v 0 {aqSpatialArgs} {aqTemporalArgs} -c:a copy -c:s copy \"{outputFile}\"";
        }
    }
}
