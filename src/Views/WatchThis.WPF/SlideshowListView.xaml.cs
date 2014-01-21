using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WatchThis.Controllers;
using WatchThis.Models;

namespace WatchThis.Wpf
{
    public partial class SlideshowListView : Window, ISlideshowListViewer, IPlatformService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        IList<SlideshowModel> _slideshowModels = new List<SlideshowModel>();
        public SlideshowModel NewSlideshow { get; set; }
        string _lastAddedPath;

        public SlideshowListView()
        {
            NewSlideshow = new SlideshowModel();
            InitializeComponent();

            SlideshowList.Focus();

            SlideshowEnumerator.FindSlideshows(
                Preferences.Instance.SlideshowwPath,
                this,
                this);
        }

        private void OnMouseDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            InvokeOnUiThread(delegate { RunSelectedSlideshow(); });
        }

        private void RunSelectedSlideshow()
        {
            SlideshowModel model = null;
            if (TabControl.SelectedItem == CreatedTabItem)
            {
                model = NewSlideshow;
            }
            else
            {
                var index = SlideshowList.SelectedIndex;
                if (index >= 0 && index < _slideshowModels.Count)
                {
                    model = _slideshowModels[index];
                }
            }

            if (model != null)
            {
                logger.Info("Run {0}", model.Name);
                var show = new SlideshowWindow(model);
                show.Show();
            }
        }

        private void RunSlideshow(object sender, ExecutedRoutedEventArgs args)
        {
            logger.Info("RunSlideshow");
            RunSelectedSlideshow();
        }

        private void SaveAs(object sender, ExecutedRoutedEventArgs args)
        {
            logger.Info("SaveAs");
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = SlideshowModel.Extension;
            dialog.InitialDirectory = Preferences.Instance.SlideshowwPath;
            dialog.Filter = "Slideshow documents (*.slideshow)|*.slideshow";
            var response = dialog.ShowDialog();
            if (response == true)
            {
                try
                {
                    var filename = dialog.FileName;
                    NewSlideshow.Name = Path.GetFileNameWithoutExtension(filename);
                    NewSlideshow.Save(filename);

                    SlideshowEnumerator.FindSlideshows(Preferences.Instance.SlideshowwPath, this, this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format("Failed saving '{0}'; {1}", dialog.FileName, ex.Message),
                        "Failed saving slideshow",
                        MessageBoxButton.OK);
                }
            }
        }

        private void SetSlideshowFolder(object sender, ExecutedRoutedEventArgs args)
        {
            logger.Info("SetSlideshowFolder");
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var start = Preferences.Instance.SlideshowwPath;
            if (!Directory.Exists(start))
            {
                start = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
            dialog.SelectedPath = start;
            dialog.Description = string.Format("Choose a folder with WatchThis slideshow files in it ({0})", SlideshowModel.Extension);
            if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
            {
                Preferences.Instance.SlideshowwPath = dialog.SelectedPath;

                SlideshowEnumerator.FindSlideshows(
                    Preferences.Instance.SlideshowwPath,
                    this,
                    this);

                Preferences.Instance.Save();
            }
        }

        private void AddFolder(object sender, ExecutedRoutedEventArgs args)
        {
            logger.Info("AddFolder");
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Choose a folder with images";
            dialog.SelectedPath = _lastAddedPath;
            if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
            {
                NewSlideshow.FolderList.Add(new FolderModel { Path = dialog.SelectedPath, Recursive = true });
                _lastAddedPath = dialog.SelectedPath;
            }
        }

        private void RemoveSelectedFolders(object sender, ExecutedRoutedEventArgs args)
        {
            var selected = CreatedListView.SelectedItems;
            if (selected.Count < 1)
            {
                return;
            }

            string message;
            if (selected.Count == 1)
            {
                message = string.Format("Are you sure you want to remove '{0}' from the list?", ((FolderModel)selected[0]).Path);
            }
            else 
            {
                var firstFour = new string[5];
                for (int idx = 0; idx < Math.Min(firstFour.Length - 1, selected.Count); ++idx)
                {
                    firstFour[idx] = ((FolderModel)selected[idx]).Path;
                }
                if (selected.Count >= firstFour.Length)
                {
                    firstFour[firstFour.Length - 1] = "And a few more";
                }

                message = string.Format("Are you sure you want to remove these {0} items from the list?\r\n{1}", 
                    selected.Count, string.Join("\r\n", firstFour));
            }

            var response = MessageBox.Show(
                message,
                "Confirm removing item",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (response == MessageBoxResult.Yes)
            {
                for (int idx = selected.Count - 1; idx >= 0; --idx)
                {
                    NewSlideshow.FolderList.Remove((FolderModel)selected[idx]);
                }
            }
        }

        private void Exit(object sender, ExecutedRoutedEventArgs args)
        {
            Application.Current.Shutdown();
        }

        void FolderDragEnter(object sender, DragEventArgs e)
        {
            var folders = GetDropFolders(e);
            if (folders.Count < 1)
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        void FolderDrop(object sender, DragEventArgs e)
        {
            logger.Info("Drop");
            var folders = GetDropFolders(e);
            foreach (var n in folders)
            {
                NewSlideshow.FolderList.Add(new FolderModel { Path = n, Recursive = true });
                _lastAddedPath = n;
            }
        }

        IList<string> GetDropFolders(DragEventArgs e)
        {
            var folders = new List<string>();

            var droppedNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (droppedNames != null)
            {
                foreach (var n in droppedNames)
                {
                    if (!Directory.Exists(n))
                    {
                        folders.Clear();
                        break;
                    }
                    folders.Add(n);
                }
            }
            return folders;
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
