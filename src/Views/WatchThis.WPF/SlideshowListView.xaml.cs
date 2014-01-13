using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WatchThis.Controllers;
using WatchThis.Models;

namespace WatchThis.Wpf
{
    public partial class SlideshowListView : Window, ISlideshowListViewer, IPlatformService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        IList<SlideshowModel> _slideshowModels = new List<SlideshowModel>();

        public SlideshowListView()
        {
            InitializeComponent();
            SlideshowList.Focus();

            SlideshowEnumerator.FindSlideshows(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "slideshows"),
                this,
                this);
        }

        private void OnMouseDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            InvokeOnUiThread(delegate { RunSelectedSlideshow(); });
        }

        private void RunSelectedSlideshow()
        {
            var index = SlideshowList.SelectedIndex;
            if (index >= 0 && index < _slideshowModels.Count)
            {
                logger.Info("Run {0}", _slideshowModels[index].Name);
                var show = new SlideshowWindow(_slideshowModels[index]);
                show.Show();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
            {
                RunSelectedSlideshow();
            }
        }

        public void EnumerationCompleted(IList<SlideshowModel> slideshowModels)
        {
            _slideshowModels = slideshowModels;
            SlideshowList.ItemsSource = slideshowModels;
            if (slideshowModels.Count > 0)
            {
                SlideshowList.SelectedIndex = 0;
            }
        }

        public void InvokeOnUiThread(Action action)
        {
            Dispatcher.BeginInvoke(action);
        }
    }
}
