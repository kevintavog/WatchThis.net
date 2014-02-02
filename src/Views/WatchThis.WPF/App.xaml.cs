using NLog;
using System;
using System.IO;
using System.Windows;
using WatchThis.Models;

namespace WatchThis.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            Window main = null;
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                logger.Info("Command line argument: {0}", args[1]);
                try
                {
                    var model = SlideshowModel.ParseFile(args[1]);
                    main = new SlideshowWindow(model);
                }
                catch (Exception ex)
                {
                    logger.Error("Failed loading '{0}': {1}", args[1], ex);
                    MessageBox.Show(
                        string.Format("Unable to open {0}: {1}", args[1], ex.Message),
                        "Error loading file",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            if (main == null)
            {
                main = new SlideshowListView();
            }
            main.Show();
        }

        public App()
        {
            Preferences.Load(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WatchThis",
                "preferences.xml"));
        }
    }
}
