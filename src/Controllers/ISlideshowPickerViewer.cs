using WatchThis.Models;

namespace WatchThis.Controllers
{
	public interface ISlideshowPickerViewer
	{
		string ChooseFolder(string message);
		QuestionResponseType AskQuestion(string caption, string question);
		void ShowMessage(string caption, string message);
		string GetValueFromUser(string caption, string message, string defaultValue);

		void RunSlideshow(SlideshowModel model);

		bool IsEditActive { get; }
		bool IsSavedActive { get; }
		SlideshowModel SelectedSavedModel { get; }
	}

	public enum QuestionResponseType
	{
		Yes,
		No,
		Cancel
	}
}
