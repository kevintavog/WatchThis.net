using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace WatchThis
{

	public partial class SlideshowWindow : MonoMac.AppKit.NSWindow
	{
		#region Constructors
		// Called when created from unmanaged code
		public SlideshowWindow(IntPtr handle) : base (handle)
		{
			Initialize();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public SlideshowWindow(NSCoder coder) : base (coder)
		{
			Initialize();
		}
		// Shared initialization code
		void Initialize()
		{
		}
		#endregion
	}
}

