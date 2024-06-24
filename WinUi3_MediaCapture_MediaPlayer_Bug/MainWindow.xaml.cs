using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUi3_MediaCapture_MediaPlayer_Bug
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MediaCapture mediaCapture;
        private MediaFrameSourceGroup selectedSource;
        private VideoEncodingProperties selectedResolution;
        private ObservableCollection<string> log = new();

        public MainWindow()
        {
            this.InitializeComponent();

            mediaPlayerElement.MediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            mediaPlayerElement.MediaPlayer.SourceChanged += MediaPlayer_SourceChanged;
            mediaPlayerElement.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            LoadWebcamsAsync();
        }

        private async void LoadWebcamsAsync()
        {
            var groups = await MediaFrameSourceGroup.FindAllAsync();
            webcamComboBox.ItemsSource = groups;
            webcamComboBox.SelectedItem = groups.FirstOrDefault();
        }

        private async void WebcamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedSource = (MediaFrameSourceGroup)webcamComboBox.SelectedItem;
            await InitializeMediaCaptureAsync();

            var resolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview)
                                       .Select(p => p as VideoEncodingProperties)
                                       .ToList();
            resolutionComboBox.ItemsSource = resolutions;
            resolutionComboBox.SelectedItem = resolutions.FirstOrDefault();
        }

        private async void ResolutionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedResolution = (VideoEncodingProperties)resolutionComboBox.SelectedItem;
            if (selectedResolution != null)
            {
                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, selectedResolution);
                DispatcherQueue.TryEnqueue(() => log.Add($"MediaCapture resolution selected: {selectedResolution.Subtype}"));

                // RGB24, UYVY ({59565955-0000-0010-8000-00AA00389B71}), I420 ({30323449-0000-0010-8000-00AA00389B71}), ...
                // do not work with MediaPlayerElement, but they did work with WinUI 2 CaptureElement!
                // MediaPlayer just returns State Paused. No Error.
            }
        }

        private async System.Threading.Tasks.Task InitializeMediaCaptureAsync()
        {
            mediaCapture?.Dispose();

            mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = selectedSource.Id
            };
            await mediaCapture.InitializeAsync(settings);

            var frameSource = mediaCapture.FrameSources[selectedSource.SourceInfos[0].Id];
            mediaPlayerElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
            DispatcherQueue.TryEnqueue(() => log.Add($"MediaCapture source selected: {selectedSource.DisplayName}"));
        }

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(() => log.Add($"Media player state changed: {sender.CurrentState}"));
        }

        private void MediaPlayer_SourceChanged(MediaPlayer sender, object args)
        {
            DispatcherQueue.TryEnqueue(() => log.Add("Media player source changed"));
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() => log.Add($"Media player failed: {args.Error} {args.ErrorMessage}"));
        }
    }
}
