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
		public string SlideshowwPath { get; set; }

		private Preferences()
		{
			SlideshowwPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
				"slideshows");
		}

		static public Preferences Load(string filename)
		{
			Instance = new Preferences();
			if (File.Exists(filename))
			{
				try
				{
					var xml = XDocument.Parse(File.ReadAllText(filename));
					Instance.SlideshowwPath = xml.GetValue("SlideshowPath", Instance.SlideshowwPath);
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
					new XElement("SlideshowPath", Instance.SlideshowwPath)));

			File.WriteAllText(filename, xml.ToString());
		}
	}
}

