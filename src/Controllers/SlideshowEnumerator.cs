using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using NLog;
using WatchThis.Models;
using WatchThis.Utilities;

namespace WatchThis.Controllers
{
	public class SlideshowEnumerator
	{
		private string Folder { get; set; }
		private ISlideshowListViewer Viewer { get; set; }
		private IPlatformService PlatformService { get; set; }
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private SlideshowEnumerator()
		{
		}

		static public void FindSlideshows(string folder, ISlideshowListViewer viewer, IPlatformService platformService)
		{
			new SlideshowEnumerator { Folder = folder, Viewer = viewer, PlatformService = platformService }.Begin();
		}

		private void Begin()
		{
			Task.Factory.StartNew( () => 
				{
					Queue<string> filenames = new Queue<string>();
					try
					{
						FileEnumerator.AddFilenames(
							filenames,
							Folder,
							(s) => Path.GetExtension(s).Equals(SlideshowModel.Extension, StringComparison.InvariantCultureIgnoreCase));
					}
					catch (Exception ex)
					{
						logger.Info("Exception enumerating folder '{0}'; {1}", Folder, ex);
					}

					var slideshows = new List<SlideshowModel>();
					foreach (var name in filenames)
					{
						try
						{
							var model = SlideshowModel.ParseFile(name);
							slideshows.Add(model);
						}
						catch (Exception ex)
						{
							logger.Error("Exception parsing '{0}': {1}", name, ex);
						}
					}

					PlatformService.InvokeOnUiThread( delegate { Viewer.EnumerationCompleted(slideshows); } );
				});
		}
	}
}

