using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using WatchThis.Models;

namespace WatchThis.Controllers
{
	internal enum DriverState
	{
		Created,
		Enumerating,
		Playing,
		Paused,
		Stopped,
	}

	/// <summary>
	/// Responsible for loading and playing slideshows, though delegating to a viewer for the actual
	/// image display.
	/// All calls to the viewer are on the main/UI thread in order to minimize threading issues in
	/// the various UI implementations.
	/// </summary>
	public class SlideshowDriver
	{
		public SlideshowModel Model { get; private set; }
		public ISlideshowViewer Viewer { get; private set; }
		public IPlatformService PlatformService { get; private set; }

		private DriverState State { get; set; }

		private Timer _timer;
		private Random _random = new Random();
		private IList<ImageInformation> _recent = new List<ImageInformation>();
		private int? _recentIndex;
		private static Logger logger = LogManager.GetCurrentClassLogger();


		static public SlideshowDriver Create(string filename, ISlideshowViewer viewer, IPlatformService platformService)
		{
			return Create(SlideshowModel.ParseFile(filename), viewer, platformService);
		}

		static public SlideshowDriver Create(SlideshowModel model, ISlideshowViewer viewer, IPlatformService platformService)
		{
			var driver = new SlideshowDriver(model, viewer, platformService);
			driver.BeginEnumerate();
			return driver;
		}

		private SlideshowDriver(SlideshowModel model, ISlideshowViewer viewer, IPlatformService platformService)
		{
			PlatformService = platformService;
			Model = model;
			Viewer = viewer;
			State = DriverState.Created;
		}

		public void Play()
		{
			logger.Info("SlideshowDriver.Play: {0}", State);
			if (State == DriverState.Playing)
			{
				return;
			}

			State = DriverState.Playing;
			var time = (Model.SlideSeconds + Model.TransitionSeconds) * 1000;
			if (time < 100)
			{
				time = 100;
			}
			_timer = new Timer(time) { AutoReset = true, Enabled = true };
			_timer.Elapsed += (s, e) => NextSlide();

			Next();
		}

		public void Pause()
		{
			logger.Info("SlideshowDriver.Pause: {0}", State);
			State = DriverState.Paused;
			if (_timer != null)
			{
				_timer.Stop();
				_timer.Dispose();
				_timer = null;
			}
		}

		public void Stop()
		{
			logger.Info("SlideshowDriver.Stop {0}", State);
			State = DriverState.Stopped;
			if (_timer != null)
			{
				_timer.Stop();
				_timer = null;
			}
		}

		public void Next()
		{
			logger.Info("SlideshowDriver.Next {0}", State);
			NextSlide();
		}

		private void NextSlide()
		{
			Task.Factory.StartNew( () =>
			{
				if (Model.ImageList.Count == 0)
				{
					BeginEnumerate();
				}
				else
				{
					_timer.Stop();
					_timer.Start();
					if (_recentIndex.HasValue)
					{
						++_recentIndex;
						if (_recentIndex < _recent.Count)
						{
							DisplayRequest(_recent[_recentIndex.Value]);
						}
						else
						{
							_recentIndex = null;
						}
					}

					if (!_recentIndex.HasValue)
					{
						DisplayRequest(NextRandom());
					}
				}
			});
		}

		public void Previous()
		{
			logger.Info("SlideshowDriver.Previous {0}", State);
			int index;
			if (_recentIndex.HasValue)
			{
				index = _recentIndex.Value - 1;
			}
			else
			{
				// The last item (-1) is currently being displayed. -2 is the previous item
				index = _recent.Count - 2;
			}
			if (index < 0)
			{
				return;
			}

			_timer.Stop();
			_timer.Start();
			_recentIndex = index;
			DisplayRequest(_recent[_recentIndex.Value]);
		}

		private ImageInformation NextRandom()
		{
			var index = _random.Next(Model.ImageList.Count);
			var item = Model.ImageList[index];
			Model.ImageList.RemoveAt(index);

			_recent.Add(item);
			while (_recent.Count > 1000)
			{
				_recent.RemoveAt(0);
			}

			return item;
		}

		// Ask the view to display an image
		private void DisplayRequest(ImageInformation info)
		{
			PlatformService.InvokeOnUiThread( delegate { Viewer.DisplayImage(info); } );
		}

		private void BeginEnumerate()
		{
			State = DriverState.Enumerating;
			Task.Factory.StartNew( () => 
				{
					Model.Enumerate( () => 
						{
							PlatformService.InvokeOnUiThread( delegate { Viewer.ImagesAvailable(); } );
						});
				})
				.ContinueWith( t =>
				{
					if (t.IsCompleted)
					{
						if (State == DriverState.Playing || State == DriverState.Paused)
						{
							PlatformService.InvokeOnUiThread( delegate 
								{
									Viewer.ImagesLoaded(); 
								} );
						}
						else
						{
							logger.Info("Not playing, ignoring completed enumeration");
						}
					}
					else
					{
						logger.Error("Failed or canceled: {0}", t.Exception);
						PlatformService.InvokeOnUiThread( delegate { Viewer.Error(t.Exception.Message); });
					}

					return t;
				});
		}
	}
}

