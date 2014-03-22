using System;
using System.IO;
using WatchThis.Models;
using MonoMac.Foundation;
using MonoMac.AppKit;
using NLog;
using System.Reflection;

namespace WatchThis
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private bool startedSlideshow;
		private ShowListController controller;


		public AppDelegate()
		{
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			return true;
		}

		public override NSApplicationTerminateReply ApplicationShouldTerminate(NSApplication sender)
		{
			var close = (controller != null) ? controller.WindowShouldClose() : true;
			return close ? NSApplicationTerminateReply.Now : NSApplicationTerminateReply.Cancel;
		}

		public override void FinishedLaunching(NSObject notification)
		{
			// On the Mac, we are running on Mono - emit the Mono revision
			var monoVersion = "None";
			Type monoRuntimeType;
			MethodInfo getDisplayNameMethod;
			if ((monoRuntimeType = typeof(object).Assembly.GetType("Mono.Runtime")) != null &&
				(getDisplayNameMethod = monoRuntimeType.GetMethod(
					"GetDisplayName",
					BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding, null,
					Type.EmptyTypes, null)) != null)
			{
				monoVersion = string.Format("Mono {0}", getDisplayNameMethod.Invoke(null, null));
			}

			logger.Info("Finished launching: .NET {0} ({1}); startedSlideshow = {2}", System.Environment.Version, monoVersion, startedSlideshow);
			if (!startedSlideshow)
			{
				Preferences.Load(Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Library",
					"Preferences",
					"com.rangic.WatchThis.xml"));

				controller = new ShowListController();
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

