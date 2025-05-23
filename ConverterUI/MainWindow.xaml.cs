using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using FfmpegConverter.Encoders;
using FfmpegConverter.Ffmpeg;

namespace ConverterUI
{
    public partial class MainWindow : Window
    {
        private EncoderConfig config;
        private List<string> filesToConvert = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            bool configCreated;
            config = EncoderConfig.LoadOrCreate(out configCreated);
            config.ResetSkippedVersionIfOutdated();
        }

        private void SelectFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Video Files|*.mp4;*.avi;*.mov;*.mkv;*.wmv;*.flv;*.webm;*.asf;*.m4p;*.m4v;*.mpg;*.mp2;*.mpeg;*.mpe;*.mpv;*.m2v;*.3gp;*.m2ts;*.ts;*.mts;*.3g2"
            };
            if (dlg.ShowDialog() == true)
            {
                filesToConvert = dlg.FileNames.ToList();
                FilesList.ItemsSource = filesToConvert;
                ConvertButton.IsEnabled = filesToConvert.Count > 0;
            }
        }

        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            ConvertButton.IsEnabled = false;
            OutputBox.Clear();

            string gpuVendor = NvidiaRadio.IsChecked == true ? "nvidia" : "intel";

            // Check for update (optional, can be commented out if not needed)
            await Task.Run(() => Updater.CheckAndPromptUpdate(config));

            if (filesToConvert.Count == 0)
            {
                OutputBox.AppendText("No video files selected.\n");
                return;
            }

            foreach (var file in filesToConvert)
            {
                await Task.Run(() =>
                {
                    AppendOutput($"Converting: {file}\n");
                    FfmpegProcessRunner.ConvertWithFfmpeg(
                        file,
                        gpuVendor,
                        config,
                        progress => AppendOutput(progress) // This will update OutputBox
                    );
                });
            }

            AppendOutput("Conversion complete.\n");
            ConvertButton.IsEnabled = true;
        }

        // Thread-safe output appending
        private void AppendOutput(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputBox.AppendText(text);
                OutputBox.ScrollToEnd();
            });
        }
    }
}