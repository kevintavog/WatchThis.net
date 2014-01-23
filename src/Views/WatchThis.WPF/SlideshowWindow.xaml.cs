using NLog;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WatchThis.Controllers;
using WatchThis.Models;
using WatchThis.Utilities;

namespace WatchThis.Wpf
{
    public partial class SlideshowWindow : Window, ISlideshowViewer, IPlatformService, INotifyPropertyChanged
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public SlideshowDriver Driver { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsMaximized { get { return WindowState == WindowState.Maximized; } }
        public bool IsNormal { get { return !IsMaximized; } }

        private System.Windows.Threading.DispatcherTimer _hideControlsTimer = new System.Windows.Threading.DispatcherTimer();
        private int _lastMouseMoveTime;
        private Point _lastMouseMovePosition = new Point();

        public SlideshowWindow(SlideshowModel model)
        {
            InitializeComponent();

            _hideControlsTimer.Interval = new TimeSpan(0, 0, 2);
            _hideControlsTimer.Tick += (s, e) =>
            {
                var diff = Environment.TickCount - _lastMouseMoveTime;
                if (diff > _hideControlsTimer.Interval.TotalMilliseconds)
                {
                    HideControls();
                }
            };

            var screenSize = this.ScreenDimensions();
            Width = screenSize.Width * 0.75;
            Height = screenSize.Height * 0.75;
            Left = screenSize.Left + ((screenSize.Width - Width) / 2);
            Top = screenSize.Top + ((screenSize.Height - Height) / 2);

            // TODO: Display a 'Loading images...' message
            Driver = SlideshowDriver.Create(model, this, this);
            this.FirePropertyChanged(PropertyChanged, () => Driver);
            ShowControls();
        }

        private void Close(object sender, ExecutedRoutedEventArgs e)
        {
            logger.Info("Close");
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            Driver.Stop();
            Driver = null;
            base.OnClosed(e);
        }

        private void PreviousImage(object sender, ExecutedRoutedEventArgs e)
        {
            logger.Info("PreviousImage");
            Driver.Previous();
            this.FirePropertyChanged(PropertyChanged, () => Driver);
        }

        private void PauseResume(object sender, ExecutedRoutedEventArgs e)
        {
            logger.Info("PauseResume");
            Driver.PauseOrResume();
            this.FirePropertyChanged(PropertyChanged, () => Driver);
        }

        private void NextImage(object sender, ExecutedRoutedEventArgs e)
        {
            logger.Info("NextImage");
            Driver.Next();
            this.FirePropertyChanged(PropertyChanged, () => Driver);
        }

        private void ToggleFullScreen(object sender, ExecutedRoutedEventArgs e)
        {
            logger.Info("ToggleFullScreen");
            if (IsMaximized)
            {
                WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                WindowState = System.Windows.WindowState.Normal;
            }
            else
            {
                WindowStyle = System.Windows.WindowStyle.None;
                WindowState = System.Windows.WindowState.Maximized;
            }

            ChangedFullScreenState();
        }

        private void ShowControls()
        {
            Cursor = Cursors.Arrow;
            Controls.Visibility = Visibility.Visible;
            _hideControlsTimer.Stop();
            _hideControlsTimer.Start();
        }

        private void HideControls()
        {
            Cursor = Cursors.None;
            Controls.Visibility = Visibility.Hidden;
        }

        private void ChangedFullScreenState()
        {
            this.FirePropertyChanged(PropertyChanged, () => IsMaximized);
            this.FirePropertyChanged(PropertyChanged, () => IsNormal);
        }

        private void WindowMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            if (position.Equals(_lastMouseMovePosition))
            {
                return;
            }

            _lastMouseMovePosition = position;
            _lastMouseMoveTime = e.Timestamp;

            if (Controls.Visibility != Visibility.Visible)
            {
                ShowControls();
            }
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs args)
        {
            logger.Info("SizeChanged to {0}", args.NewSize);
            Canvas.SetLeft(ControlsPanel, (args.NewSize.Width - ControlsPanel.ActualWidth) / 2);
        }

        public void DisplayImage(ImageInformation imageInfo)
        {
            logger.Info("Show image: {0}", imageInfo.FullPath);

            BitmapImage source = null;
            Task.Factory.StartNew(() =>
            {
                source = new BitmapImage();
                source.BeginInit();
                source.UriSource = new Uri(imageInfo.FullPath);
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.EndInit();

                // To allow the image to be used by the UI thread...
                source.Freeze();
            })
            .ContinueWith( (t) =>
            {
                InvokeOnUiThread(() => Image.Source = source);
                return t;
            });
        }

        public void Error(string message)
        {
            // TODO: Inform the user, somehow
            logger.Error("Driver reported an error: {0}", message);
        }

        public void ImagesAvailable()
        {
            logger.Info("ImagesAvailable: {0}", Driver.Model.ImageList.Count);
            Driver.Play();
        }

        public void ImagesLoaded()
        {
            logger.Info("ImagesLoaded: {0}", Driver.Model.ImageList.Count);
            Driver.Play();
        }

        public void InvokeOnUiThread(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }

    }
}
