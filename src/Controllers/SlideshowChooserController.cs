using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using WatchThis.Models;
using WatchThis.Utilities;

namespace WatchThis.Controllers
{
	public class SlideshowChooserController : ISlideshowListViewer, INotifyPropertyChanged
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public IPlatformService PlatformService { get; private set; }
		public ISlideshowPickerViewer Viewer { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;
		public SlideshowModel EditedSlideshow 
		{
			get { return _editedSlideshow; } 
			private set
			{
				_editedSlideshow = value;
				PlatformService.InvokeOnUiThread(delegate 
					{
						this.FirePropertyChanged(PropertyChanged, () => EditedSlideshow);
					});
			} 
		}
		public IList<SlideshowModel> SavedSlideshows 
		{ 
			get { return _savedSlideshows; }

			private set
			{
				_savedSlideshows = value;
				PlatformService.InvokeOnUiThread(delegate 
					{
						this.FirePropertyChanged(PropertyChanged, () => SavedSlideshows);
					});
			}
		}


		private IList<SlideshowModel> _savedSlideshows;
		private SlideshowModel _editedSlideshow;



		public SlideshowChooserController(ISlideshowPickerViewer viewer, IPlatformService platformService)
		{
			Viewer = viewer;
			PlatformService = platformService;
			EditedSlideshow =  new SlideshowModel();
			SavedSlideshows = new List<SlideshowModel>();
		}

		public void RunSlideshow()
		{
			SlideshowModel model = null;
			if (Viewer.IsEditActive)
			{
				logger.Info("Run slideshow from edit tab");
				model = EditedSlideshow;
			}
			else
			if (Viewer.IsSavedActive)
			{
				model = Viewer.SelectedSavedModel;
				logger.Info("Run saved slideshow {0}", model.Filename);
			}
			else
			{
				logger.Info("Neither saved nor edit is active");
			}

			if (model != null)
			{
				Viewer.RunSlideshow(model);
			}
		}

		public void AddEditFolder()
		{
			var folder = Viewer.ChooseFolder("Select folder");
			logger.Info("Add Edit folder: '{0}'", folder);
			if (null != folder)
			{
				EditedSlideshow.FolderList.Add(new FolderModel { Path = folder, Recursive = true });
			}
        }

		public void AddEditDroppedFolders(IList<string> paths)
		{
			foreach (var s in paths)
			{
				EditedSlideshow.FolderList.Add(new FolderModel { Path = s, Recursive = true });
			}
		}

		public void RemoveEditFolders(IList<FolderModel> selectedFolders)
		{
			if (selectedFolders.Count < 1)
			{
				return;
			}

			string message;
			if (selectedFolders.Count == 1)
			{
				message = string.Format("Are you sure you want to remove '{0}' from the list?", selectedFolders[0].Path);
			}
			else 
			{
				var firstFew = new string[5];
				for (int idx = 0; idx < Math.Min(firstFew.Length - 1, selectedFolders.Count); ++idx)
				{
					firstFew[idx] = selectedFolders[idx].Path;
				}
				if (selectedFolders.Count >= firstFew.Length)
				{
					firstFew[firstFew.Length - 1] = string.Format("And a {0} more", selectedFolders.Count - firstFew.Length);
				}

				message = string.Format("Are you sure you want to remove the {0} selected items from the list?\r\n{1}", 
					selectedFolders.Count, string.Join("\r\n", firstFew));
			}

			if (Viewer.AskQuestion("Verify remove", message))
			{
				foreach (var fm in selectedFolders)
				{
					logger.Info("Remove selected folder {0}", fm.Path);
					EditedSlideshow.FolderList.Remove(fm);
				}
			}
		}

		public void ClearEdit()
		{
			logger.Info("ClearEdit");
			if (Viewer.IsEditActive)
			{
				if (EditedSlideshow.FolderList.Count < 1 || 
					Viewer.AskQuestion("Verify clear", "Are you sure you want to clear the current slideshow?"))
				{
					EditedSlideshow.Reset();
				}
			}
		}

		/// <summary>
		/// Deletes the edited slideshow if the edited tab is active, otherwise deletes the currently selected
		/// saved slideshow.
		/// Expected to be called on UI thread
		/// </summary>
		public void DeleteSlideshow()
		{
			logger.Info("DeleteSlideshow");
			if (Viewer.IsSavedActive)
			{
				var model = Viewer.SelectedSavedModel;
				if (model != null)
				{
					if (AskQuestion("Are you sure you want to delete the slideshow '{0}'?", model.Name))
					{
						try
						{
							File.Delete(model.Filename);
							this.FirePropertyChanged(PropertyChanged, () => SavedSlideshows);
						}
						catch (Exception ex)
						{
							logger.Error("Exception deleting '{0}': {1}", model.Name, ex);
							ShowMessage("Error deleting '{0}': {1}", model.Name, ex.Message);
						}
					}
				}

			}
			else if (Viewer.IsEditActive)
			{
				logger.Error("TODO: delete & clear the edited slideshow");
				// If it's been saved, ask for confirmation
			}
		}

		public void FindSavedSlideshows()
		{
			SlideshowEnumerator.FindSlideshows(
				Preferences.Instance.SlideshowwFolder,
				this,
				PlatformService);
		}

		public void EnumerationCompleted(IList<SlideshowModel> slideshowModels)
		{
			logger.Info("Saved slideshow enumeration completed with {0} found", slideshowModels.Count);
			SavedSlideshows = slideshowModels;
		}

		private bool AskQuestion(string caption, string message, params object[] args)
		{
			return Viewer.AskQuestion(caption, string.Format(message, args));
		}

		private void ShowMessage(string caption, string message, params object[] args)
		{
			Viewer.ShowMessage(caption, string.Format(message, args));
		}
	}
}

