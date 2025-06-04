using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FfmpegConverter.Encoders
{
    public enum IntelQualityMode
    {
        CQP,
        ICQ
    }

    [DataContract]
    internal class IntelEncoderOptions
    {
        [DataMember] public int CqpValue { get; set; } = 40;
        [DataMember] public int IcqValue { get; set; } = 21;

        // This property is not serialized directly
        public IntelQualityMode QualityMode { get; set; } = IntelQualityMode.CQP;

        // This property is serialized instead, as a string
        [DataMember(Name = "QualityMode")]
        private string QualityModeString
        {
            get { return QualityMode.ToString().ToLowerInvariant(); }
            set
            {
                if (string.Equals(value, "icq", System.StringComparison.OrdinalIgnoreCase))
                    QualityMode = IntelQualityMode.ICQ;
                else
                    QualityMode = IntelQualityMode.CQP;
            }
        }

        [DataMember] public bool EnableTenBit { get; set; } = true;
        [DataMember] public bool ForceCpuDecode { get; set; } = false;
    }
}