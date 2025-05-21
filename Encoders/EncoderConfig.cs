using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class EncoderConfig
    {
        [DataMember] public string GpuVendor { get; set; } = "nvidia"; // "nvidia" or "intel"
        [DataMember] public NvidiaEncoderOptions Nvidia { get; set; } = new NvidiaEncoderOptions();
        [DataMember] public IntelEncoderOptions Intel { get; set; } = new IntelEncoderOptions();

        public static string ConfigFileName => "config.json";

        public static EncoderConfig LoadOrCreate(out bool created)
        {
            if (!File.Exists(ConfigFileName))
            {
                var config = new EncoderConfig();
                config.Save();
                created = true;
                return config;
            }
            using (var stream = File.OpenRead(ConfigFileName))
            {
                var ser = new DataContractJsonSerializer(typeof(EncoderConfig));
                created = false;
                return (EncoderConfig)ser.ReadObject(stream);
            }
        }

        public void Save()
        {
            var ser = new DataContractJsonSerializer(typeof(EncoderConfig));
            using (var stream = new MemoryStream())
            {
                // Create an indented JSON writer
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream, Encoding.UTF8, ownsStream: false, indent: true))
                {
                    ser.WriteObject(writer, this);
                    writer.Flush();
                    // Write the formatted JSON to file
                    File.WriteAllText(ConfigFileName, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
        }
    }
}