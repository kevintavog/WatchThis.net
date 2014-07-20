using System;
using WatchThis.Models;

namespace WatchThis.Controllers
{
	public interface ISlideshowViewer
	{
		/// <summary>
		/// Called from a worker thread so the UI thread is not blocked
		/// </summary>
        object LoadImage(MediaItem item);

		/// <summary>
		/// Called from the UI thread to display the previously loaded image.
		/// Returns a string to be emitted to the log with the filename of the loaded image.
		/// </summary>
		string DisplayImage(object image);


		void DisplayInfo(string message);

		void Error(string message);

		void UpdateUiState();
	}
}

