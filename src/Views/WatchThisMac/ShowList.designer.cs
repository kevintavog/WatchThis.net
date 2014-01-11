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
		MonoMac.AppKit.NSTableView tableView { get; set; }

		[Action ("runSlideshow:")]
		partial void runSlideshow (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
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
