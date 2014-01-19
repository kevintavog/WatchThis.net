// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace WatchThis
{
	[Register ("ShowListController")]
	partial class ShowListController
	{
		[Outlet]
		MonoMac.AppKit.NSTableView folderTableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField slideDuration { get; set; }

		[Outlet]
		MonoMac.AppKit.NSStepper slideDurationStepper { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView tableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTabView tabView { get; set; }

		[Action ("addNewFolder:")]
		partial void addNewFolder (MonoMac.Foundation.NSObject sender);

		[Action ("openSlideshowFolder:")]
		partial void openSlideshowFolder (MonoMac.Foundation.NSObject sender);

		[Action ("removeNewFolder:")]
		partial void removeNewFolder (MonoMac.Foundation.NSObject sender);

		[Action ("runNewSlideshow:")]
		partial void runNewSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("runSlideshow:")]
		partial void runSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("saveSlideshow:")]
		partial void saveSlideshow (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (folderTableView != null) {
				folderTableView.Dispose ();
				folderTableView = null;
			}

			if (slideDuration != null) {
				slideDuration.Dispose ();
				slideDuration = null;
			}

			if (slideDurationStepper != null) {
				slideDurationStepper.Dispose ();
				slideDurationStepper = null;
			}

			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}

			if (tabView != null) {
				tabView.Dispose ();
				tabView = null;
			}
		}
	}

	[Register ("ShowList")]
	partial class ShowList
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
