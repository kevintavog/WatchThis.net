using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.IO;
using NLog;
using WatchThis.Models;
using WatchThis.Controllers;

namespace WatchThis
{
	public partial class ShowListController : MonoMac.AppKit.NSWindowController, ISlideshowListViewer, IPlatformService
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private IList<SlideshowModel> _slideshows = new List<SlideshowModel>();

#region Constructors

		// Called when created from unmanaged code
		public ShowListController(IntPtr handle) : base(handle)
		{
			Initialize();
		}
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public ShowListController(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		public ShowListController() : base("ShowList")
		{
			Initialize();
		}

		void Initialize()
		{
			SlideshowEnumerator.FindSlideshows(
				Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.Personal),
					"Pictures",
					"slideshows"),
				this,
				this);
		}

#endregion

		public new ShowList Window
		{
			get
			{
				return (ShowList)base.Window;
			}
		}

		public override void WindowDidLoad()
		{
			base.WindowDidLoad();

			tableView.DoubleClick += (sender, e) => runSlideshow();
		}

		partial void runSlideshow(MonoMac.Foundation.NSObject sender)
		{
			runSlideshow();
		}


		[Export("doubleAction")]
		public void runSlideshow()
		{
			logger.Info("runSlideshow {0}", _slideshows[tableView.SelectedRow].Name);

			var controller = new SlideshowWindowController();
			controller.Model = _slideshows[tableView.SelectedRow];
			controller.Window.MakeKeyAndOrderFront(this);
		}

		[Export("numberOfRowsInTableView:")]
		public int numberOfRowsInTableView(NSTableView tv)
		{
			return _slideshows.Count;
		}

		[Export("tableView:objectValueForTableColumn:row:")]
		public string objectValueForTableColumn(NSTableView table, NSTableColumn column, int rowIndex)
		{
			var model = _slideshows[rowIndex];
			return model.Name;
		}

		public void EnumerationCompleted(IList<SlideshowModel> slideshowModels)
		{
			logger.Info("EnumerationCompleted; found {0} slideshows", slideshowModels.Count);

			_slideshows = slideshowModels;
			tableView.ReloadData();
		}

		public void InvokeOnUiThread(Action action)
		{
			BeginInvokeOnMainThread(delegate { action(); } );
		}
	}
}

