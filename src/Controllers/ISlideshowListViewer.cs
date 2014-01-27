using System;
using WatchThis.Models;
using System.Collections.Generic;

namespace WatchThis.Controllers
{
	public interface ISlideshowListViewer
	{
		void EnumerationCompleted(IList<SlideshowModel> slideshowModels);
	}
}

