using System;
using System.IO;
using WatchThis.Models;
using MonoMac.Foundation;
using MonoMac.AppKit;
using NLog;

namespace WatchThis
{

	public partial class AppDelegate : NSApplicationDelegate
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private bool startedSlideshow;


		public AppDelegate()
		{
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			return true;
		}

		public override void FinishedLaunching(NSObject notification)
		{
			logger.Info("Finished launching: startedSlideshow = {0}", startedSlideshow);
			if (!startedSlideshow)
			{
				Preferences.Load(Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Library",
					"Preferences",
					"com.rangic.WatchThis.xml"));

				var controller = new ShowListController();
				controller.Window.MakeKeyAndOrderFront(this);
			}
		}

		public override bool OpenFile(NSApplication sender, string filename)
		{
			logger.Info("App.OpenFile: {0}", filename);
			try
			{
				var model = SlideshowModel.ParseFile(filename);
				if (model != null)
				{
					var controller = new SlideshowWindowController();
					controller.Model = model;
					controller.Window.MakeKeyAndOrderFront(this);
					startedSlideshow = true;
					return true;
				}
				else
				{
					logger.Info("Failed loading '{0}'", filename);
				}
			}
			catch (Exception e)
			{
				logger.Info("Error opening file: {0}", e);
			}

			return true;
		}
	}
}

