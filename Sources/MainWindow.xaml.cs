using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace WpfCamera
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MediaCapture _mediaCapture = new MediaCapture();
        private readonly CaptureElement _captureElement;
        private StorageFolder _captureFolder;
        private bool _initialized = false;

        private bool isRecording;
        public bool IsRecording
        {
            get => isRecording;
            private set
            {
                titleStatusTimer.Enabled = value;
                isRecording = value;

                if (isRecording)
                {
                    LogAction("VIDEO - RECORDING STARTED");
                    btnVideo.Content = "STOP recording video";
                }
                else
                {
                    LogAction("VIDEO - RECORDING STOPPED");
                    btnVideo.Content = "Record video";
                }
            }
        }

        private readonly System.Timers.Timer titleStatusTimer;

        public ObservableCollection<string> Logs { get; }

        public MainWindow()
        {
            InitializeComponent();
            Logs = new ObservableCollection<string>();

            _captureElement = new CaptureElement
            {
                Stretch = Windows.UI.Xaml.Media.Stretch.Uniform
            };
            _captureElement.Loaded += CaptureElement_Loaded;
            _captureElement.Unloaded += CaptureElement_Unloaded;

            XamlHost.Child = _captureElement;

            DataContext = this;

            LogAction("MainWindow()");

            titleStatusTimer = new System.Timers.Timer();
            titleStatusTimer.Elapsed += TitleStatusTimer_Elapsed;
            titleStatusTimer.Interval = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
        }

        private async void CaptureElement_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await _mediaCapture.StopPreviewAsync();
            LogAction("CaptureElement_Unloaded ... StopPreviewAsync");
        }

        private async void CaptureElement_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!_initialized)
            {
                var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                // Fall back to the local app storage if the Pictures Library is not available
                _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;
                // Get available devices for capturing pictures
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                if (allVideoDevices.Count > 0)
                {
                    // try to find back camera
                    DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);

                    // If there is no device mounted on the back panel, return the first device found
                    var device = desiredDevice ?? allVideoDevices.FirstOrDefault();

                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings() { VideoDeviceId = device.Id });
                    _captureElement.Source = _mediaCapture;

                    _initialized = true;
                }

                LogAction("CaptureElement_Loaded ... starting initialize capture device");
            }

            if (_initialized)
            {
                LogAction("CaptureElement_Loaded ... Initializing");
                await _mediaCapture.StartPreviewAsync();
                LogAction("CaptureElement_Loaded ... show preview");
            }
        }

        private async void TitleStatusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string message = "WPF camera | STREAM DATA: ";

            Stopwatch sw = Stopwatch.StartNew();
            using (var stream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                message += stream.Size;
            }

            message += $" ({(int)sw.Elapsed.TotalMilliseconds} ms.)";

            Dispatcher.Invoke(() => Title = message);
        }

        private async void Video_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Video_Click");

            if (!_initialized)
            {
                return;
            }

            if (IsRecording)
            { // stop recording
                IsRecording = false;
                await _mediaCapture.StopRecordAsync();
            }
            else
            { // start recording
                var videoFile = await _captureFolder.CreateFileAsync(GetCameraFileName(".wmv"), CreationCollisionOption.GenerateUniqueName);

                var encodingProfile = MediaEncodingProfile.CreateWmv(VideoEncodingQuality.Auto);

                await _mediaCapture.StartRecordToStorageFileAsync(encodingProfile, videoFile);

                IsRecording = true;
            }
        }

        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Photo_Click");

            if (!_initialized)
            {
                return;
            }

            await CreateCameraImage();
        }

        private void Audio_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Audio_Click");

            /*
            if (!_initialized)
            {
                return;
            }

            if (_isRecording)
            { // stop recording
                _isRecording = false;
                await _mediaCapture.StopRecordAsync();
                LogAction("AUDIO - RECORDING STOPPED");

                btnAudio.Content = "Record audio";
            }
            else
            { // start recording
                var audioFile = await _captureFolder.CreateFileAsync(GetCameraFileName(".mp3"), CreationCollisionOption.GenerateUniqueName);

                var encodingProfile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Medium);

                await _mediaCapture.StartRecordToStorageFileAsync(encodingProfile, audioFile);

                _isRecording = true;

                LogAction("AUDIO - RECORDING STARTED");
                btnAudio.Content = "STOP recording audio";

            }
            */
        }

        private void LogAction(string message)
        {
            Logs.Add($"[{DateTime.Now.ToShortTimeString()}] {message}");
        }

        private string GetCameraFileName(string extension)
        {
            string name = DateTime.Now.ToString("yyyyMMdd_HH-mm").Replace(".", "_").Replace(":", "_");
            return name + extension;
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string cmd = "explorer.exe";
            string arg = _captureFolder.Path + "\\";
            Process.Start(cmd, arg);
        }

        private async Task CreateCameraImage()
        {
            using var stream = new InMemoryRandomAccessStream();

            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

            try
            {
                var file = await _captureFolder.CreateFileAsync(GetCameraFileName(".jpg"), CreationCollisionOption.GenerateUniqueName);

                var decoder = await BitmapDecoder.CreateAsync(stream);

                using var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);

                await encoder.FlushAsync();
            }
            catch (Exception)
            {
            }
        }
    }
}
