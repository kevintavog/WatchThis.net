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
using MonoMac.CoreImage;

namespace WatchThis
{
	public partial class SlideshowWindowController : MonoMac.AppKit.NSWindowController, ISlideshowViewer, IPlatformService
	{
		public SlideshowModel Model { get; set; }

		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Random random = new Random();
		NSImageView		imageView;
		private SlideshowDriver driver;

		CATransition typeTransition;
		CATransition filterTransition;

		CIBarsSwipeTransition				barsSwipeTransition;
		CICopyMachineTransition 			copyMachineTransition;
		CIDisintegrateWithMaskTransition	disintegrateTransition;
		CIDissolveTransition				dissolveTransition;
		CIFlashTransition 					flashTransition;
		CIModTransition						modTransition;
		CIPageCurlTransition 				pageCurlTransition;
		CIRippleTransition 					rippleTransition;
		CISwipeTransition					swipeTransition;

		CIImage								transitionInputMaskImage;
		CILanczosScaleTransform				disintegrateTransform;

		string[] transitionTypes = new string []
		{ 
			CATransition.TransitionMoveIn,
			CATransition.TransitionPush,
			CATransition.TransitionReveal
		};

		IList<CIFilter> transitionFilters = new List<CIFilter>();


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

			imageView = CreateImageView();
			contentView.AddSubview(imageView);

			base.AwakeFromNib();

			CreateTransitions();
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

			// To work around possible problems in Mono's NSImage dispose: https://bugzilla.xamarin.com/show_bug.cgi?id=15081
			System.GC.Collect();

			var image = new NSImage(imageInfo.FullPath);
			var imageRep = image.BestRepresentationForDevice(null);
			SizeF imageSize = new SizeF(imageRep.PixelsWide, imageRep.PixelsHigh);
			image.Size = imageSize;
			imageView.Image = image;
			var filterName = ApplyFilter(fromLeft);
			logger.Info("image {0}; {1}", imageInfo.FullPath, filterName);
		}

		private string ApplyFilter(bool fromLeft)
		{
			var index = random.Next(transitionTypes.Count() + transitionFilters.Count);
			if (index < transitionTypes.Count())
			{
				typeTransition.Type = transitionTypes[index];
				typeTransition.Subtype = fromLeft ? CATransition.TransitionFromLeft : CATransition.TransitionFromRight;
				typeTransition.FillMode = CAFillMode.Forwards;
				imageView.Layer.AddAnimation(typeTransition, null);

				return typeTransition.Type;
			}
			else
			{
				filterTransition.filter = transitionFilters[index - transitionTypes.Count()];
				UpdateFilterProperties();
				imageView.Layer.AddAnimation(filterTransition, null);

				return ((CIFilter) filterTransition.filter).Attributes[(NSString) "CIAttributeFilterName"].ToString();
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

		private void UpdateFilterProperties()
		{
			RectangleF rect = Window.ContentView.Frame;
			var extent = new CIVector(rect.Left, rect.Bottom, rect.Width, rect.Height);
			var center = new CIVector(rect.GetMidX(),rect.GetMidY());

			pageCurlTransition.Extent = extent;

			copyMachineTransition.Extent = extent;

			var xScale = rect.Width / transitionInputMaskImage.Extent.Width;
			var yScale = rect.Height / transitionInputMaskImage.Extent.Height;
			disintegrateTransform.Scale = yScale;
			disintegrateTransform.AspectRatio = xScale / yScale;
			disintegrateTransition.Mask = (CIImage) disintegrateTransform.ValueForKey((NSString) "outputImage");

			flashTransition.Center = center;
			flashTransition.Extent = extent;

			modTransition.Center = center;

			rippleTransition.Center = center;
			rippleTransition.Extent = extent;
		}

		private void CreateTransitions()
		{
			var bundle = NSBundle.MainBundle;

			// Shading & mask for transitions (borrowed from the "Fun House" Core Image example).
			var inputShadingImage = new CIImage(NSData.FromFile(bundle.PathForResource("restrictedshine", "tiff")));
			transitionInputMaskImage = new CIImage(NSData.FromFile(bundle.PathForResource("transitionmask", "jpg")));



			typeTransition = new CATransition();
			typeTransition.Duration = driver != null ? driver.Model.TransitionSeconds : 0.5;
			typeTransition.Type = CATransition.TransitionPush;
			typeTransition.Subtype = CATransition.TransitionFromLeft;
			typeTransition.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseInEaseOut);

			barsSwipeTransition = new CIBarsSwipeTransition();
			barsSwipeTransition.SetDefaults();

			copyMachineTransition = new CICopyMachineTransition();
			copyMachineTransition.SetDefaults();

			pageCurlTransition = new CIPageCurlTransition();
			pageCurlTransition.SetDefaults();
			pageCurlTransition.Angle = (float) (Math.PI / 4);
			pageCurlTransition.ShadingImage = inputShadingImage;
			pageCurlTransition.BacksideImage = transitionInputMaskImage;

			disintegrateTransform = new CILanczosScaleTransform();
			disintegrateTransform.SetDefaults();
			disintegrateTransform.Image = transitionInputMaskImage;
			disintegrateTransition = new CIDisintegrateWithMaskTransition();
			disintegrateTransition.SetDefaults();

			dissolveTransition = new CIDissolveTransition();
			dissolveTransition.SetDefaults();

			flashTransition = new CIFlashTransition();
			flashTransition.SetDefaults();

			modTransition = new CIModTransition();
			modTransition.SetDefaults();

			rippleTransition = new CIRippleTransition();
			rippleTransition.SetDefaults();
			rippleTransition.ShadingImage = inputShadingImage;

			swipeTransition = new CISwipeTransition();
			swipeTransition.SetDefaults();

			filterTransition = new CATransition();
			filterTransition.filter = disintegrateTransition;
			filterTransition.Duration = typeTransition.Duration;


			transitionFilters.Add(barsSwipeTransition);
			transitionFilters.Add(copyMachineTransition);
			transitionFilters.Add(disintegrateTransition);
			transitionFilters.Add(dissolveTransition);
			transitionFilters.Add(flashTransition);
			transitionFilters.Add(modTransition);
			transitionFilters.Add(pageCurlTransition);
			transitionFilters.Add(rippleTransition);
			transitionFilters.Add(swipeTransition);
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

