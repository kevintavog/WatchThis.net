using System;
using WatchThis.Models;

namespace WatchThis.Controllers
{
	public interface ISlideshowViewer
	{
		void DisplayImage(ImageInformation imageInfo);

		void Error(string message);

		void ImagesAvailable();
		void ImagesLoaded();
	}
}

