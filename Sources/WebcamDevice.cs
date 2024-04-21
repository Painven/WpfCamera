using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

public class WebcamDevice : INotifyPropertyChanged
{
    private readonly MediaCapture _mediaCapture = new MediaCapture();
    private readonly CaptureElement _captureElement;
    private StorageFolder _captureFolder;

    private bool _initialized = false;

    private bool isRecording;

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsRecording
    {
        get => isRecording;
        private set
        {
            isRecording = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecording)));
        }
    }

    public string GetFilesStorage => _captureFolder.Path;
    public CaptureElement GetCaptureElement => _captureElement;

    public WebcamDevice()
    {
        _captureElement = new CaptureElement
        {
            Stretch = Windows.UI.Xaml.Media.Stretch.Uniform
        };
        _captureElement.Loaded += CaptureElement_Loaded;
        _captureElement.Unloaded += CaptureElement_Unloaded;
    }

    private async void CaptureElement_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        await _mediaCapture.StopPreviewAsync();
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

        }

        if (_initialized)
        {
            await _mediaCapture.StartPreviewAsync();
        }
    }

    public async Task ToggleVideoRecorder()
    {
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

    public async Task ToggleAudioRecorder()
    {
        await Task.Yield();
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

    public async Task CreateCameraImage(string imageName = null)
    {
        using var stream = new InMemoryRandomAccessStream();

        await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

        try
        {
            var file = await _captureFolder.CreateFileAsync(imageName ?? GetCameraFileName(".jpg"), CreationCollisionOption.GenerateUniqueName);

            var decoder = await BitmapDecoder.CreateAsync(stream);

            using var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);

            await encoder.FlushAsync();
        }
        catch (Exception)
        {
        }
    }

    private string GetCameraFileName(string extension)
    {
        string name = DateTime.Now.ToString("yyyyMMdd_HH-mm").Replace(".", "_").Replace(":", "_");
        return name + extension;
    }
}
