using System;
using System.IO;
using WatchThis.Models;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace WatchThis
{

	public partial class AppDelegate : NSApplicationDelegate
	{
		public AppDelegate()
		{
		}

		public override void FinishedLaunching(NSObject notification)
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
}

