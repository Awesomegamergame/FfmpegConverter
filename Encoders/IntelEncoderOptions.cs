using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class IntelEncoderOptions
    {
        [DataMember] public int CqpValue { get; set; } = 40;

    }
}