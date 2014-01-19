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
		private SlideshowModel _newSlideshow = new SlideshowModel();
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
		}

#endregion

		public new ShowList Window
		{
			get
			{
				return (ShowList)base.Window;
			}
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			Window.RegisterForDraggedTypes(new string[] { NSPasteboard.NSFilenamesType } );
			Window.DropAction = (l) => DroppedPaths(l);

			tableView.DoubleClick += (sender, e) => runSlideshow();
			slideDurationStepper.Activated += (sender, e) => StepperDidChange();
			UpdateControls();

			SlideshowEnumerator.FindSlideshows(
				Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.Personal),
					"Pictures",
					"slideshows"),
				this,
				this);
		}

		partial void runNewSlideshow(MonoMac.Foundation.NSObject sender)
		{
			var table = tabView.Selected.Identifier.ToString().Equals("New") ? folderTableView : tableView;
			runSlideshow(table);
		}

		partial void runSlideshow(MonoMac.Foundation.NSObject sender)
		{
			var table = sender as NSTableView;
			if (table != null)
			{
				SlideshowModel model = null;
				switch (table.Tag)
				{
					case 0:
						model = _newSlideshow;
						break;
					case 1:
						model = _slideshows[tableView.SelectedRow];
						break;
				}

				if (model != null)
				{
					RunSlideshowModel(model);
				}
			}
		}

		void StepperDidChange()
		{
			_newSlideshow.SlideSeconds = slideDurationStepper.IntValue;
			UpdateControls();
		}

		private void UpdateControls()
		{
			slideDurationStepper.IntValue = (int)_newSlideshow.SlideSeconds;
			slideDuration.IntegerValue = (int)_newSlideshow.SlideSeconds;
		}

		[Export("doubleAction")]
		public void runSlideshow()
		{
			runSlideshow(tabView.Selected.Identifier.ToString().Equals("New") ? folderTableView : tableView);
		}

		void RunSlideshowModel(SlideshowModel model)
		{
			var controller = new SlideshowWindowController();
			controller.Model = model;
			controller.Window.MakeKeyAndOrderFront(this);
		}

		[Export("numberOfRowsInTableView:")]
		public int numberOfRowsInTableView(NSTableView tv)
		{
			switch (tv.Tag)
			{
				case 0:
					return _newSlideshow.FolderList.Count;
				case 1:
					return _slideshows.Count;
			}

			logger.Error("Unexpected numberOfRowsinTableView: {0}", tv.Tag);
			return 0;
		}

		[Export("tableView:objectValueForTableColumn:row:")]
		public string objectValueForTableColumn(NSTableView table, NSTableColumn column, int rowIndex)
		{
			switch (table.Tag)
			{
				case 0:
					return _newSlideshow.FolderList[rowIndex].Path;
				case 1:
					return _slideshows[rowIndex].Name;
			}

			return "<Error!>";
		}

		private void DroppedPaths(IList<string> paths)
		{
			foreach (var s in paths)
			{
				_newSlideshow.FolderList.Add(new FolderModel { Path = s, Recursive = true });
			}
			folderTableView.ReloadData();
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

