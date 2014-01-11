using System;
using System.Collections.Generic;
using WatchThis.Models;

namespace WatchThis.Controllers
{
	public interface ISlideshowListViewer
	{
		void EnumerationCompleted(IList<SlideshowModel> slideshowModels);
	}
}

