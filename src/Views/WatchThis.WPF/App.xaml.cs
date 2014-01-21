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
        public App()
        {
            Preferences.Load(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WatchThis",
                "preferences.xml"));
        }
    }
}
