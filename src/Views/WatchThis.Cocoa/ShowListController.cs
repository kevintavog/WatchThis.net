using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.IO;
using NLog;
using WatchThis.Models;
using WatchThis.Controllers;
using WatchThis.Utilities;

namespace WatchThis
{
	public partial class ShowListController : MonoMac.AppKit.NSWindowController, ISlideshowPickerViewer, IPlatformService
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private SlideshowChooserController _controller;

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
			_controller = new SlideshowChooserController(this, this);
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

			savedTableView.DoubleClick += (sender, e) => runSlideshow();
			slideDurationStepper.Activated += (sender, e) => StepperDidChange();

			NotifyPropertyChangedHelper.SetupPropertyChanged(_controller, ObjectChanged);

			_controller.FindSavedSlideshows();
		}

		private void ObjectChanged(object sender, string propertyName)
		{
			var handled = false;
			if (sender == _controller)
			{
				switch (propertyName)
				{
					case "SavedSlideshows":
						savedTableView.ReloadData();
						handled = true;
						break;

					case "EditedSlideshow":
						slideDurationStepper.IntValue = (int)_controller.EditedSlideshow.SlideSeconds;
						slideDuration.IntegerValue = (int)_controller.EditedSlideshow.SlideSeconds;
						handled = true;
						break;

					default:
						break;
				}
			}
			else if (sender == _controller.EditedSlideshow)
			{
				switch (propertyName)
				{
					case "SlideSeconds":
						slideDurationStepper.IntValue = (int)_controller.EditedSlideshow.SlideSeconds;
						slideDuration.IntegerValue = (int)_controller.EditedSlideshow.SlideSeconds;
						handled = true;
						break;

					default:
						break;
				}
			}
			else if (sender == _controller.EditedSlideshow.FolderList)
			{
				switch (propertyName)
				{
					case "collection":
						folderTableView.ReloadData();
						handled = true;
						break;

					default:
						break;
				}
			}

			if (!handled)
			{
				logger.Info("Unhandled property updated: {0} [{1}]", propertyName, sender.GetType().Name);
			}
		}

#region ISlideshowPickerViewer
		public string ChooseFolder(string message)
		{
			var openPanel = new NSOpenPanel
			{
				ReleasedWhenClosed = true,
				Prompt = message,
				CanChooseDirectories = true,
				CanChooseFiles = false
			};

			var result = (NsButtonId)openPanel.RunModal();
			if (result == NsButtonId.OK)
			{
				return openPanel.Url.Path;
			}
			return null;
		}

		public bool AskQuestion(string message)
		{
			var alert = NSAlert.WithMessage(message, "No", "Yes", "Cancel", "");
			var response = (NSAlertType)alert.RunSheetModal(Window);
			logger.Info("responded {0} to {1}", response, message);
			return response == NSAlertType.AlternateReturn;
		}

		public void ShowMessage(string message)
		{
			NSAlert.WithMessage(message, "Close", "", "", "").RunSheetModal(Window);
		}

		public void RunSlideshow(SlideshowModel model)
		{
			var controller = new SlideshowWindowController();
			controller.Model = model;
			controller.Window.MakeKeyAndOrderFront(this);
		}

		public bool IsEditActive { get { return tabView.Selected.Identifier.Equals(editTabView.Identifier); } }
		public bool IsSavedActive { get { return tabView.Selected.Identifier.Equals(savedTabView.Identifier); } }
		public SlideshowModel SelectedSavedModel { get { return _controller.SavedSlideshows[savedTableView.SelectedRow]; } }
#endregion


#region Cocoa action handlers
		partial void addFolder(MonoMac.Foundation.NSObject sender)
		{
			_controller.AddEditFolder();
		}

		partial void removeFolder(MonoMac.Foundation.NSObject sender)
		{
			logger.Info("Remove selected folders: {0}", folderTableView.SelectedRowCount);

			if (folderTableView.SelectedRow >= 0)
			{
				var selectedFolders = new List<FolderModel>();
				foreach (var index in folderTableView.SelectedRows.ToArray())
				{
					selectedFolders.Add(_controller.EditedSlideshow.FolderList[(int)index]);
				}
				_controller.RemoveEditFolders(selectedFolders);
			}
		}

		partial void runSlideshow(MonoMac.Foundation.NSObject sender)
		{
			_controller.RunSlideshow();
		}

		[Export("doubleAction")]
		public void runSlideshow()
		{
			runSlideshow(null);
		}

		partial void clearEdit(MonoMac.Foundation.NSObject sender)
		{
			_controller.ClearEdit();
		}

		partial void deleteSlideshow(MonoMac.Foundation.NSObject sender)
		{
			_controller.DeleteSlideshow();
		}

		partial void editSlideshow(MonoMac.Foundation.NSObject sender)
		{
			logger.Info("edit slideshow");
		}

		partial void openSlideshow(MonoMac.Foundation.NSObject sender)
		{
			logger.Info("open slideshow");
		}

		void StepperDidChange()
		{
			_controller.EditedSlideshow.SlideSeconds = slideDurationStepper.IntValue;
		}

		partial void activateEditTab(MonoMac.Foundation.NSObject sender)
		{
			tabView.Select(editTabView);
		}

		partial void activateSavedTab(MonoMac.Foundation.NSObject sender)
		{
			tabView.Select(savedTabView);
		}

#endregion


#if false
		partial void saveSlideshow(MonoMac.Foundation.NSObject sender)
		{
			logger.Info("Saving slideshow");
			var savePanel = new NSSavePanel
			{
				Prompt = "Save slideshow",
				CanCreateDirectories = true,
				DirectoryUrl = NSUrl.FromFilename(Preferences.Instance.SlideshowwPath)
			};

			var result = (NsButtonId)savePanel.RunModal();
			if (result == NsButtonId.OK)
			{
				try
				{
					var filename = savePanel.Url.Path;
					_newSlideshow.Name = Path.GetFileNameWithoutExtension(filename);
					_newSlideshow.Save(filename);

					RefreshSlideshows();
				}
				catch (Exception ex)
				{
					var alert = NSAlert.WithMessage(
						string.Format("Failed saving '{0}'; {1}", savePanel.Url.Path, ex.Message),
						"Close",
						"",
						"",
						"");
					alert.RunSheetModal(Window);
				}
			}
		}
#endif

#if false
		partial void openSlideshowFolder(MonoMac.Foundation.NSObject sender)
		{
			logger.Info("open slideshow folder");
			var openPanel = new NSOpenPanel
			{
				ReleasedWhenClosed = true,
				Prompt = "Select folder",
				CanChooseDirectories = true,
				CanChooseFiles = false
			};

			var result = (NsButtonId)openPanel.RunModal();
			if (result == NsButtonId.OK)
			{
				Preferences.Instance.SlideshowwPath = openPanel.Url.Path;

				RefreshSlideshows();
				Preferences.Instance.Save();
			}
		}
#endif


		[Export("numberOfRowsInTableView:")]
		public int numberOfRowsInTableView(NSTableView tv)
		{
			switch (tv.Tag)
			{
				case 0:
					return _controller.EditedSlideshow.FolderList.Count;
				case 1:
					return _controller.SavedSlideshows.Count;
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
					return _controller.EditedSlideshow.FolderList[rowIndex].Path;
				case 1:
					return _controller.SavedSlideshows[rowIndex].Name;
			}

			return "<Error!>";
		}

		private void DroppedPaths(IList<string> paths)
		{
			_controller.AddEditDroppedFolders(paths);
		}

		public void InvokeOnUiThread(Action action)
		{
			BeginInvokeOnMainThread(delegate { action(); } );
		}
	}
}

