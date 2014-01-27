using System;
using System.IO;
using NLog;
using System.Xml.Linq;
using WatchThis.Utilities;

namespace WatchThis.Models
{
	public class Preferences
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		static public Preferences Instance { get; private set; }
		public string SlideshowwFolder { get; set; }

        private string Filename { get; set; }

		private Preferences()
		{
			SlideshowwFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
				"WatchThis Slideshows");
		}

        public void Save()
        {
            Save(Filename);
        }

		static public Preferences Load(string filename)
		{
			Instance = new Preferences();
            Instance.Filename = filename;
			if (File.Exists(filename))
			{
				try
				{
					var xml = XDocument.Parse(File.ReadAllText(filename));
					Instance.SlideshowwFolder = xml.GetValue("SlideshowFolder", Instance.SlideshowwFolder);
				}
				catch (Exception e)
				{
					logger.Error("Exception loading preferences (using defaults): {0}", e);
				}
			}
			else
			{
				try
				{
					Save(filename);
				}
				catch (Exception e)
				{
					logger.Error("Exception saving preferences to '{0}': {1}", filename, e);
				}
			}
			return Instance;
		}

		static public void Save(string filename)
		{
			var xml = new XDocument(
				new XElement("com.rangic.WatchThis.Preferences",
					new XElement("SlideshowFolder", Instance.SlideshowwFolder)));

			File.WriteAllText(filename, xml.ToString());
		}
	}
}

