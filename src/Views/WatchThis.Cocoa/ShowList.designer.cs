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
		MonoMac.AppKit.NSTabViewItem editTabView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView folderTableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView savedTableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTabViewItem savedTabView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField slideDuration { get; set; }

		[Outlet]
		MonoMac.AppKit.NSStepper slideDurationStepper { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTabView tabView { get; set; }

		[Action ("activateEditTab:")]
		partial void activateEditTab (MonoMac.Foundation.NSObject sender);

		[Action ("activateSavedTab:")]
		partial void activateSavedTab (MonoMac.Foundation.NSObject sender);

		[Action ("addFolder:")]
		partial void addFolder (MonoMac.Foundation.NSObject sender);

		[Action ("clearEdit:")]
		partial void clearEdit (MonoMac.Foundation.NSObject sender);

		[Action ("deleteSlideshow:")]
		partial void deleteSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("editSlideshow:")]
		partial void editSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("openSlideshow:")]
		partial void openSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("removeFolder:")]
		partial void removeFolder (MonoMac.Foundation.NSObject sender);

		[Action ("runSlideshow:")]
		partial void runSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("saveSlideshow:")]
		partial void saveSlideshow (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (editTabView != null) {
				editTabView.Dispose ();
				editTabView = null;
			}

			if (folderTableView != null) {
				folderTableView.Dispose ();
				folderTableView = null;
			}

			if (savedTableView != null) {
				savedTableView.Dispose ();
				savedTableView = null;
			}

			if (savedTabView != null) {
				savedTabView.Dispose ();
				savedTabView = null;
			}

			if (slideDuration != null) {
				slideDuration.Dispose ();
				slideDuration = null;
			}

			if (slideDurationStepper != null) {
				slideDurationStepper.Dispose ();
				slideDurationStepper = null;
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
