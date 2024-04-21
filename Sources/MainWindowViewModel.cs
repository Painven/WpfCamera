using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace WpfCamera
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly WebcamDevice webcam = new WebcamDevice();
        private readonly System.Timers.Timer titleStatusTimer;

        private string title;

        public string Title
        {
            get => title;
            set
            {
                if (title == value) return;
                title = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public CaptureElement CaptureElement => webcam.GetCaptureElement;

        public ICommand RecordAudioCommand { get; }
        public ICommand RecordVideoCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand CreatePhotoCommand { get; }

        public MainWindowViewModel()
        {
            Stopwatch sw = Stopwatch.StartNew();

            LogAction("MainWindowViewModel() START");

            webcam = new WebcamDevice();
            RecordAudioCommand = new RelayCommand<object>(async (o) => await RecordAudio(o));
            RecordVideoCommand = new RelayCommand<object>(async (o) => await RecordVideo(o));
            CreatePhotoCommand = new RelayCommand<object>(async (o) => await CreatePhoto(o));
            OpenFolderCommand = new RelayCommand<object>(OpenFolder);




            titleStatusTimer = new System.Timers.Timer();
            titleStatusTimer.Elapsed += TitleStatusTimer_Elapsed;
            titleStatusTimer.Interval = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;

            webcam.PropertyChanged += Webcam_PropertyChanged;
            RaisePropertyChanged(nameof(CaptureElement));

            LogAction($"MainWindowViewModel() END [{sw.Elapsed.TotalMilliseconds} ms.]");
        }

        private void Webcam_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WebcamDevice.IsRecording))
            {
                LogAction($"Recording status changed - now {webcam.IsRecording}");
                titleStatusTimer.Enabled = webcam.IsRecording;
            }
        }

        private void TitleStatusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            /*
            string message = "WPF camera | STREAM DATA: ";

            Stopwatch sw = Stopwatch.StartNew();
            using (var stream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                message += stream.Size;
            }

            message += $" ({(int)sw.Elapsed.TotalMilliseconds} ms.)";

            Dispatcher.Invoke(() => Title = message);
            */
        }

        private async Task RecordVideo(object o)
        {
            LogAction("Video CLICKED");
            await webcam.ToggleVideoRecorder();
        }

        private async Task RecordAudio(object o)
        {
            LogAction("Audio CLICKED");
            await webcam.ToggleAudioRecorder();
        }

        private async Task CreatePhoto(object o)
        {
            LogAction("Photo CLICKED");
            await webcam.CreateCameraImage();
        }

        private void LogAction(string message)
        {
            Logs.Add($"[{DateTime.Now.ToShortTimeString()}] {message}");
        }


        private void OpenFolder(object o)
        {
            FileExplorerHelper.RevealInFileExplorer(webcam.GetFilesStorage);
        }
    }
}
