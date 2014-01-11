using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using MonoMac.AppKit;
using MonoMac.CoreAnimation;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using NLog;
using WatchThis.Controllers;
using WatchThis.Models;

namespace WatchThis
{
	public partial class SlideshowWindowController : MonoMac.AppKit.NSWindowController, ISlideshowViewer, IPlatformService
	{
		public SlideshowModel Model { get; set; }

		private static Logger logger = LogManager.GetCurrentClassLogger();
		NSImageView[]	imageViews;
		int 			activeViewIndex = -1;
		private SlideshowDriver driver;

		CATransition transition;


		#region Constructors
		// Called when created from unmanaged code
		public SlideshowWindowController(IntPtr handle) : base (handle)
		{
			Initialize();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public SlideshowWindowController(NSCoder coder) : base (coder)
		{
			Initialize();
		}
		// Call to load from the XIB/NIB file
		public SlideshowWindowController() : base ("SlideshowWindow")
		{
			Initialize();
		}
		// Shared initialization code
		void Initialize()
		{
		}
		#endregion


		public new SlideshowWindow Window
		{
			get
			{
				return (SlideshowWindow)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			driver = SlideshowDriver.Create(Model, this, this);

			Window.BackgroundColor = NSColor.Black;

			var contentView = Window.ContentView;
			contentView.WantsLayer = true;

			imageViews = new NSImageView[2];
			for (int idx = 0; idx < imageViews.Length; ++idx)
			{
				imageViews[idx] = CreateImageView();
			}
			contentView.AddSubview(imageViews[0]);

			transition = new CATransition();
			transition.Duration = driver != null ? driver.Model.TransitionSeconds : 0.5;
			transition.Type = CATransition. TransitionPush;
			transition.Subtype = CATransition.TransitionFromLeft;
			transition.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseOut);

			contentView.Animations = new NSDictionary("subviews", transition);

			base.AwakeFromNib();

			Window.Delegate = new SlideshowWindowDelegate(this);
		}

		public void WindowWillClose()
		{
			driver.Stop();
		}

		partial void nextImage(MonoMac.Foundation.NSObject sender)
		{
			driver.Next();
		}

		partial void previousImage(MonoMac.Foundation.NSObject sender)
		{
			driver.Previous();
		}


		public void Error(string message)
		{
			logger.Error("Error from driver: '{0}'", message);
		}

		public void ImagesAvailable()
		{
			logger.Info("Images available: {0}", driver.Model.ImageList.Count);
			driver.Play();
		}

		public void ImagesLoaded()
		{
			logger.Info("Images fully loaded: {0}", driver.Model.ImageList.Count);
			driver.Play();
		}

		public void DisplayImage(ImageInformation imageInfo)
		{
			SetImage(imageInfo, false);
		}

		private void SetImage(ImageInformation imageInfo, bool fromLeft)
		{
			NSCursor.SetHiddenUntilMouseMoves(true);

			NSImageView priorView = null;
			if (activeViewIndex != -1)
			{
				priorView = imageViews[activeViewIndex];
			}

			activeViewIndex = (activeViewIndex + 1) % imageViews.Length;

			// To work around possible problems in Mono's NSImage dispose: https://bugzilla.xamarin.com/show_bug.cgi?id=15081
			System.GC.Collect();

			var image = new NSImage(imageInfo.FullPath);
			var imageRep = image.BestRepresentationForDevice(null);
			SizeF imageSize = new SizeF(imageRep.PixelsWide, imageRep.PixelsHigh);
			image.Size = imageSize;
			logger.Info("View {0}, image {1}", activeViewIndex, imageInfo.FullPath);


			var currentView = imageViews[activeViewIndex];
			currentView.Image = image;

			if (priorView != null)
			{
				if (driver.Model.TransitionSeconds > 0)
				{
					transition.Subtype = fromLeft ? CATransition.TransitionFromLeft : CATransition.TransitionFromRight;
					currentView.Frame = priorView.Frame;
					NSView animator = (NSView) Window.ContentView.Animator;
					animator.ReplaceSubviewWith(priorView, currentView);
				}
				else
				{
					currentView.Frame = priorView.Frame;
					Window.ContentView.ReplaceSubviewWith(priorView, currentView);
				}
			}
		}

		private NSImageView CreateImageView()
		{
			var imageView = new NSImageView(Window.ContentView.Frame);

			imageView.ImageScaling = NSImageScale.ProportionallyDown;
			imageView.AutoresizingMask = 
				NSViewResizingMask.HeightSizable |
				NSViewResizingMask.WidthSizable;

			return imageView;
		}

		public void InvokeOnUiThread(Action action)
		{
			BeginInvokeOnMainThread(delegate { action(); } );
		}
	}

	class SlideshowWindowDelegate : NSWindowDelegate
	{
		private SlideshowWindowController controller;

		public SlideshowWindowDelegate(SlideshowWindowController controller)
		{
			this.controller = controller;
		}

		public override void WillClose(NSNotification notification)
		{
			controller.WindowWillClose();
		}
	}
}

