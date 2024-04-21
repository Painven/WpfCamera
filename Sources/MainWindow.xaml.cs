using System.Windows;

namespace WpfCamera
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = new MainWindowViewModel();
            captureViewer.Child = (DataContext as MainWindowViewModel).CaptureElement;
        }
    }
}
