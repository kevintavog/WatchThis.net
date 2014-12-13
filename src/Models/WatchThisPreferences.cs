using System;
using Rangic.Utilities.Preferences;
using System.IO;
using Rangic.Utilities.Os;

namespace WatchThis.Models
{
    public class WatchThisPreferences : BasePreferences
    {
        public string SlideshowFolder { get; set; }
        public string LastEditedFilename { get { return Path.Combine(Platform.UserDataFolder("WatchThis"), "LastEdited"); } }

        public WatchThisPreferences()
        {
            SlideshowFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "WatchThis Slideshows");
        }

        public override void FromJson(dynamic json)
        {
            SlideshowFolder = json.SlideshowFolder;
        }

        public override dynamic ToJson()
        {
            return new
            {
                SlideshowFolder = SlideshowFolder
            };
        }
    }
}

