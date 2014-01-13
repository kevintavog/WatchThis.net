using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace WatchThis
{

	public partial class AppDelegate : NSApplicationDelegate
	{
		public AppDelegate()
		{
		}

		public override void FinishedLaunching(NSObject notification)
		{
#if true
			var controller = new ShowListController();
			controller.Window.MakeKeyAndOrderFront(this);
#else
			var slideshowWindowController = new SlideshowWindowController();
			slideshowWindowController.Window.MakeKeyAndOrderFront(this);
#endif
		}
	}
}

