using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace FfmpegConverter.Encoders
{
    [DataContract]
    internal class ProgramSettings
    {
        [DataMember] public string SkippedVersion { get; set; } = "";
    }

    [DataContract]
    internal class EncoderConfig
    {
        [DataMember] public ProgramSettings Program { get; set; } = new ProgramSettings();
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

        public void ResetSkippedVersionIfOutdated()
        {
            var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (!string.IsNullOrEmpty(Program.SkippedVersion) && Program.SkippedVersion.StartsWith("v"))
            {
                Version skipVer;
                if (Version.TryParse(Program.SkippedVersion.Substring(1), out skipVer))
                {
                    if (current > skipVer)
                    {
                        Program.SkippedVersion = "";
                        Save();
                    }
                }
            }
        }
    }
}