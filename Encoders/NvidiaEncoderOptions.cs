using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class NvidiaEncoderOptions
    {
        [DataMember] public int CqValue { get; set; } = 30;
        [DataMember] public bool EnableTenBit { get; set; } = true;
        [DataMember] public bool EnableSpatialAq { get; set; } = false;
        [DataMember] public int AqStrength { get; set; } = 8;
        [DataMember] public bool EnableTemporalAq { get; set; } = false;
        [DataMember] public bool ForceCpuDecode { get; set; } = false;
        [DataMember] public bool CopySubtitles { get; set; } = true;
    }
}