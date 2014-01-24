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
	[Register ("SlideshowWindowController")]
	partial class SlideshowWindowController
	{
		[Outlet]
		MonoMac.AppKit.NSView controlsView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton previousButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton nextButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton playButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton pauseButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton closeButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton enterFullScreenButton { get; set; }

		[Outlet]
		MonoMac.AppKit.NSButton exitFullScreenButton { get; set; }

		[Action ("nextImage:")]
		partial void nextImage (MonoMac.Foundation.NSObject sender);

		[Action ("pauseResume:")]
		partial void pauseResume (MonoMac.Foundation.NSObject sender);

		[Action ("previousImage:")]
		partial void previousImage (MonoMac.Foundation.NSObject sender);
		
		[Action ("closeSlideshow:")]
		partial void closeSlideshow (MonoMac.Foundation.NSObject sender);

		[Action ("toggleFullScreen:")]
		partial void toggleFullScreen (MonoMac.Foundation.NSObject sender);

		void ReleaseDesignerOutlets ()
		{
			if (previousButton != null) {
				previousButton.Dispose ();
				previousButton = null;
			}
		}
	}

	[Register ("SlideshowWindow")]
	partial class SlideshowWindow
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
