namespace FfmpegConverter.Ffmpeg
{
    internal interface IFfmpegHardwareEncoder<TOptions>
    {
        string Name { get; }
        string BuildArguments(string inputFile, string outputFile, TOptions options, string codecName);
    }
}