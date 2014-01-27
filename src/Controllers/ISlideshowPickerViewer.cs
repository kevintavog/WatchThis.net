using WatchThis.Models;

namespace WatchThis.Controllers
{
	public interface ISlideshowPickerViewer
	{
		string ChooseFolder(string message);
		bool AskQuestion(string caption, string question);
		void ShowMessage(string caption, string message);

		void RunSlideshow(SlideshowModel model);

		bool IsEditActive { get; }
		bool IsSavedActive { get; }
		SlideshowModel SelectedSavedModel { get; }
	}
}
