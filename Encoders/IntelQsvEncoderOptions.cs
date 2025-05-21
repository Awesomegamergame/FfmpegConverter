using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class IntelQsvEncoderOptions
    {
        [DataMember] public int CqValue { get; set; } = 30;
    }
}