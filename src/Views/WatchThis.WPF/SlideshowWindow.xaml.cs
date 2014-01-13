using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WatchThis.Controllers;
using WatchThis.Models;

namespace WatchThis.Wpf
{
    public partial class SlideshowWindow : Window, ISlideshowViewer, IPlatformService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private SlideshowDriver driver;

        public SlideshowWindow(SlideshowModel model)
        {
            InitializeComponent();

            // TODO: Display a 'Loading images...' message
            driver = SlideshowDriver.Create(model, this, this);
        }

        protected override void OnClosed(EventArgs e)
        {
            driver.Stop();
            driver = null;
            base.OnClosed(e);
        }

        public void DisplayImage(ImageInformation imageInfo)
        {
            logger.Info("Show image: {0}", imageInfo.FullPath);

            var source = new BitmapImage();
            source.BeginInit();
            source.UriSource = new Uri(imageInfo.FullPath);
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();

            Image.Source = source;
        }

        public void Error(string message)
        {
            // TODO: Inform the user, somehow
            logger.Error("Driver reported an error: {0}", message);
        }

        public void ImagesAvailable()
        {
            logger.Info("ImagesAvailable: {0}", driver.Model.ImageList.Count);
            driver.Play();
        }

        public void ImagesLoaded()
        {
            logger.Info("ImagesAvailable: {0}", driver.Model.ImageList.Count);
            driver.Play();
        }

        public void InvokeOnUiThread(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }
    }
}
