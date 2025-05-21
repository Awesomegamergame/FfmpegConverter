using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class EncoderConfig
    {
        [DataMember] public NvidiaEncoderOptions Nvidia { get; set; } = new NvidiaEncoderOptions();
        [DataMember] public IntelQsvEncoderOptions IntelQsv { get; set; } = new IntelQsvEncoderOptions();

        public static string ConfigFileName => "encoderconfig.json";

        public static EncoderConfig LoadOrCreate()
        {
            if (!File.Exists(ConfigFileName))
            {
                var config = new EncoderConfig();
                config.Save();
                return config;
            }
            using (var stream = File.OpenRead(ConfigFileName))
            {
                var ser = new DataContractJsonSerializer(typeof(EncoderConfig));
                return (EncoderConfig)ser.ReadObject(stream);
            }
        }

        public void Save()
        {
            using (var stream = File.Create(ConfigFileName))
            {
                var ser = new DataContractJsonSerializer(typeof(EncoderConfig));
                ser.WriteObject(stream, this);
            }
        }
    }
}