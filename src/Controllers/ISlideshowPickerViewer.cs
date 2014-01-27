using WatchThis.Models;

namespace WatchThis.Controllers
{
	public interface ISlideshowPickerViewer
	{
		string ChooseFolder(string message);
		bool AskQuestion(string question);
		void ShowMessage(string message);

		void RunSlideshow(SlideshowModel model);

		bool IsEditActive { get; }
		bool IsSavedActive { get; }
		SlideshowModel SelectedSavedModel { get; }
	}
}
