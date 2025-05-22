# FfmpegConverter
[![Stable Build](https://github.com/Awesomegamergame/FfmpegConverter/actions/workflows/ReleaseBuild.yml/badge.svg)](https://github.com/Awesomegamergame/FfmpegConverter/releases)

A .NET Framework 4.8, C# Console app which converts any video file dropped onto it or in the same folder as it into a mkv file with the av1 codec using either qsv from intel or nvenc from nvidia on supported cards. This is all possible by passing command arguments to ffmpeg.

### Requirments

Please make sure that you have the latest version of ffmpeg download on your computer and added to the windows environment variables (the path variable) otherwise the program will not work.
