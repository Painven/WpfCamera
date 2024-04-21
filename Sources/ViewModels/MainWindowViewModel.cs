using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace WpfCamera.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly WebcamDevice webcam = new WebcamDevice();
        private readonly System.Windows.Threading.DispatcherTimer titleStatusTimer;
        private readonly ImageRecognizer imageRecognizer;
        private string title = "WPF camera";

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

        private readonly List<List<bool>> frameMaps = new List<List<bool>>();

        public MainWindowViewModel()
        {
            imageRecognizer = new ImageRecognizer();

            Stopwatch sw = Stopwatch.StartNew();

            LogAction("MainWindowViewModel() START");

            webcam = new WebcamDevice();
            RecordAudioCommand = new RelayCommand<object>(async (o) => await RecordAudio(o));
            RecordVideoCommand = new RelayCommand<object>(async (o) => await RecordVideo(o));
            CreatePhotoCommand = new RelayCommand<object>(async (o) => await CreatePhoto(o));
            OpenFolderCommand = new RelayCommand<object>(OpenFolder);

            titleStatusTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal, Application.Current.MainWindow.Dispatcher);
            titleStatusTimer.Interval = TimeSpan.FromSeconds(3);
            titleStatusTimer.Tick += TitleStatusTimer_Tick;

            webcam.PropertyChanged += Webcam_PropertyChanged;
            RaisePropertyChanged(nameof(CaptureElement));

            LogAction($"MainWindowViewModel() END [{sw.Elapsed.TotalMilliseconds} ms.]");

        }

        private async void TitleStatusTimer_Tick(object sender, EventArgs e)
        {
            var sb = new StringBuilder("WPF camera | STREAM DATA: ");

            Stopwatch sw = Stopwatch.StartNew();

            var image = await webcam.CreateCameraImage("temp.jpg");
            var imageBoolMap = await imageRecognizer.GetImageBoolMap(image);
            frameMaps.Add(imageBoolMap);

            if (frameMaps.Count > 2)
            {
                frameMaps.RemoveAt(0);
            }

            if (frameMaps.Any())
            {
                var content = frameMaps.Select(fm => fm.Where(i => i == true).Count().ToString());
                sb.Append($"[{string.Join(" | ", content)}] ");
                sb.Append($"({(int)sw.Elapsed.TotalMilliseconds} ms.)");
            }

            if (frameMaps.Count == 2)
            {
                bool sameImage = imageRecognizer.IsSameImage(frameMaps[0], frameMaps[1]);
                if (!sameImage)
                {
                    LogAction("Изображение изменилось!");
                }
            }

            Title = sb.ToString();
        }

        private void Webcam_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WebcamDevice.IsRecording))
            {
                LogAction($"Recording status changed - now {webcam.IsRecording}");
                if (webcam.IsRecording)
                {
                    titleStatusTimer.Start();
                }
                else
                {
                    titleStatusTimer.Stop();
                }
            }
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
            Application.Current.MainWindow.Dispatcher.Invoke(() => Logs.Add($"[{DateTime.Now.ToShortTimeString()}] {message}"));
        }

        private void OpenFolder(object o)
        {
            FileExplorerHelper.RevealInFileExplorer(webcam.GetFilesStorage);
        }
    }
}
