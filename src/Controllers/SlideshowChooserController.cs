using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
		private bool _editedHasChanged;



		public SlideshowChooserController(ISlideshowPickerViewer viewer, IPlatformService platformService)
		{
			Viewer = viewer;
			PlatformService = platformService;
			EditedSlideshow =  new SlideshowModel();
			SavedSlideshows = new List<SlideshowModel>();

			NotifyPropertyChangedHelper.SetupPropertyChanged(EditedSlideshow, (c, h) => _editedHasChanged = true);

			var filename = SlideshowModel.EnsureExtension(Preferences.Instance.LastEditedFilename);
			if (File.Exists(filename))
			{
				try
				{
					EditedSlideshow = SlideshowModel.ParseFile(filename);
					EditedSlideshow.Filename = null;
				}
				catch (Exception ex)
				{
					logger.Error("Error loading lastEdited '{0}': {1}", filename, ex);
				}
			}
		}

		public bool CanClose()
		{
			logger.Info("ChooserController.CanClose");
			if (string.IsNullOrWhiteSpace(EditedSlideshow.Filename))
			{
				// If it hasn't been saved yet, save it as the lastEdited.
				var filename = SlideshowModel.EnsureExtension(Preferences.Instance.LastEditedFilename);
				try
				{
					EditedSlideshow.Name = "";
					EditedSlideshow.Save(filename);
				}
				catch (Exception ex)
				{
					logger.Error("Error saving last edited '{0}': {1}", filename, ex);
				}
				EditedSlideshow.Filename = null;
				return true;
			}
			else
			{
				// If it's been saved before, confirm it should be saved/updated.
				return SaveIfChanged();
			}
		}

		/// <summary>
		/// If the EditedSlideshow has changes, ask the user if they want to save changes. Returns true
		/// if the caller can continue, false otherwise.
		/// True is returned if
		/// 	1) There are no changes to EditedSlideshow - or there aren't 'worthwhile' changes
		/// 	2) The user does NOT want to save
		/// 	3) The user wants to save and the save succeeds
		/// False is returned if there are worthwhile changes and:
		/// 	1) The user canceled the 'want to save' question
		/// 	2) The user canceled the request for the save name
		/// 	3) The save failed
		/// </summary>
		private bool SaveIfChanged()
		{
			if (_editedHasChanged && EditedSlideshow.FolderList.Count > 0)
			{
				var wantSave = AskQuestion("Save changes?", "Do you want to save changes to the Edited slideshow?");
				if (wantSave == QuestionResponseType.Yes)
				{
					return SaveSlideshow();
				}

				return wantSave == QuestionResponseType.No;
			}
			return true;
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
				if (model.FolderList.Count < 1)
				{
					Viewer.ShowMessage(
						"No folders", 
						"There are no folders in this slideshow, there are no images to show." 
						+ Environment.NewLine
						+ Environment.NewLine
						+ "Add some folders with images.");
				}
				else
				{
					Viewer.RunSlideshow(model);
				}
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

			if (AskQuestion("Verify remove", message) == QuestionResponseType.Yes)
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
			// TODO: Deal with unsaved edits
			logger.Info("ClearEdit");

			if (Viewer.IsEditActive)
			{
				if (SaveIfChanged())
				{
					if (EditedSlideshow.FolderList.Count < 1 || 
						AskQuestion("Verify clear", "Are you sure you want to clear the current slideshow?") == QuestionResponseType.Yes)
					{
						EditedSlideshow.Reset();
						_editedHasChanged = false;
					}
				}
			}
		}

		public bool SaveSlideshow()
		{
			logger.Info("SaveSlideshow");
			if (EditedSlideshow.FolderList.Count < 1)
			{
				Viewer.ShowMessage("Add a folder before saving", "A folder must be added in order to save a slideshow");
				return false;
			}

			var name = EditedSlideshow.Name ?? "";
			while (true)
			{
				name = Viewer.GetValueFromUser("Slideshow name", "Enter a name for the slideshow", name);
				if (name == null)
				{
					break;
				}

				if (name.Trim().Length > 0)
				{
					logger.Info("Save name = '{0}'", name);

					// Are there any conflicting names?
					var match = SavedSlideshows.FirstOrDefault(s => 
						!string.Equals(s.Filename, EditedSlideshow.Filename, StringComparison.CurrentCultureIgnoreCase) && 
						string.Equals(name, s.Name, StringComparison.CurrentCultureIgnoreCase));
					if (match != null)
					{
						ShowMessage("Name already used", "The name '{0}' is already used. Enter another name", name);
						continue;
					}

					EditedSlideshow.Name = name;

					// Report any save problems
					var priorFilename = EditedSlideshow.Filename;
					var filename = Path.Combine(
						Preferences.Instance.SlideshowwFolder,
						string.Join("_", name.Split(
							Path.GetInvalidFileNameChars(),
							StringSplitOptions.RemoveEmptyEntries)).Trim());
					try
					{
						logger.Info("Saving to '{0}'", filename);
						EditedSlideshow.Save(filename);
					}
					catch (Exception ex)
					{
						logger.Error("Save to '{0}' failed: {1}", filename, ex);
						ShowMessage("Save error", "There was an error saving the slideshow: {0}", ex.Message);
						continue;
					}

					// Was the file renamed? If so, remove the prior name
					if (priorFilename != null &&
						!string.Equals(priorFilename, EditedSlideshow.Filename, StringComparison.CurrentCultureIgnoreCase) &&
						File.Exists(priorFilename))
					{
						try
						{
							File.Delete(priorFilename);
						}
						catch (Exception ex)
						{
							logger.Error("Removal of old file '{0}' failed: {1}", priorFilename, ex);
							ShowMessage("Removal error", "There was an error removing the prior file: {0}", ex.Message);
						}
					}

					var lastEditedFilename = SlideshowModel.EnsureExtension(Preferences.Instance.LastEditedFilename);
					if (File.Exists(lastEditedFilename))
					{
						try
						{
							File.Delete(lastEditedFilename);
						}
						catch (Exception ex)
						{
							logger.Error("Error removing last edited '{0}': {1}", lastEditedFilename, ex);
						}
					}

					// Refresh slideshows
					FindSavedSlideshows();
					_editedHasChanged = false;
					return true;
				}
			}

			return false;
		}

		public void EditSlideshow()
		{
			logger.Info("edit slideshow");
//			if (SaveIfChanged())
//			{
//				// Open slideshow for edit
//			}
			Viewer.ShowMessage("Not implemented", "EditSlideshow is not implemented yet");
		}

		public void OpenSlideshow()
		{
			logger.Info("open slideshow");
//			if (SaveIfChanged()) ??
//			{
//				// Open slideshow
//			}
			Viewer.ShowMessage("Not implemented", "OpenSlideshow is not implemented yet");
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
					if (AskQuestion("Verify delete", "Are you sure you want to delete the slideshow '{0}'?", model.Name) == QuestionResponseType.Yes)
					{
						try
						{
							File.Delete(model.Filename);
							FindSavedSlideshows();
						}
						catch (Exception ex)
						{
							logger.Error("Exception deleting '{0}': {1}", model.Name, ex);
							ShowMessage("Error", "Error deleting '{0}': {1}", model.Name, ex.Message);
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
			this.FirePropertyChanged(PropertyChanged, () => SavedSlideshows);
		}

		private QuestionResponseType AskQuestion(string caption, string message, params object[] args)
		{
			return Viewer.AskQuestion(caption, string.Format(message, args));
		}

		private void ShowMessage(string caption, string message, params object[] args)
		{
			Viewer.ShowMessage(caption, string.Format(message, args));
		}
	}
}

