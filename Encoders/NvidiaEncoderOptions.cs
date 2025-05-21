using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class NvidiaEncoderOptions
    {
        [DataMember] public int CqValue { get; set; } = 30;
        [DataMember] public bool EnableSpatialAq { get; set; } = false;
        [DataMember] public int AqStrength { get; set; } = 8;
    }
}