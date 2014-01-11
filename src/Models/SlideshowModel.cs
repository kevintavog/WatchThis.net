using System;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using WatchThis.Utilities;
using NLog;

namespace WatchThis.Models
{
	public class SlideshowModel
	{
		public const string Extension = ".slideshow";

		public string Filename { set; get; }
		public string Name { set; get; }
		public string BasePath { set; get; }
		public double SlideSeconds { set; get; }
		public double TransitionSeconds { set; get; }
		public bool ManuallyControlled { set; get; }

		public bool ShowOnce { set; get; }
		public SlideOrder Order { set; get; }

		public IList<FolderModel> FolderList { get; private set; }
		public IList<Filter> FolderFilterList { get; private set; }
		public IList<Filter> FileFilterList { get; private set; }

		public List<ImageInformation> ImageList { get; set; }
		private static Logger logger = LogManager.GetCurrentClassLogger();


		public SlideshowModel()
		{
			ShowOnce = true;
			SlideSeconds = 10;
			Order = SlideOrder.Random;
			TransitionSeconds = 1;
			FolderList = new List<FolderModel>();
			FolderFilterList = new List<Filter>();
			FileFilterList = new List<Filter>();
			ImageList = new List<ImageInformation>();
		}

		static public SlideshowModel ParseFile(string filename)
		{
			var ssModel = new SlideshowModel();
			var doc = XDocument.Parse(File.ReadAllText(filename));

			ssModel.Name = doc.Root.Get("name", "");
			ssModel.BasePath = doc.Root.Get("basePath", "");
			ssModel.SlideSeconds = doc.Root.Get("slideDuration", ssModel.SlideSeconds);
			ssModel.TransitionSeconds = doc.Root.Get("transitionDuration", ssModel.TransitionSeconds);

			if (!Path.IsPathRooted(ssModel.BasePath))
			{
				var rootPath = Path.GetDirectoryName(filename);
				ssModel.BasePath = Path.GetFullPath(Path.Combine(rootPath, ssModel.BasePath));
			}

			foreach (var f in doc.Descendants("folder"))
			{
				ssModel.FolderList.Add(FolderModel.FromElement(f));
			}

			var folderFilter = doc.Descendants("folderFilter").FirstOrDefault();
			if (folderFilter != null)
			{
				foreach (var f in folderFilter.Descendants())
				{
					ssModel.FolderFilterList.Add(Filter.FromElement(f));
				}
			}

			var fileFilter = doc.Descendants("fileFilter").FirstOrDefault();
			if (fileFilter != null)
			{
				foreach (var f in fileFilter.Descendants())
				{
					ssModel.FileFilterList.Add(Filter.FromElement(f));
				}
			}
			return ssModel;
		}

		public void Enumerate(Action itemsAvailable = null)
		{
logger.Info("Enumerating...");
			List<ImageInformation> allFiles = (List<ImageInformation>)ImageList;
			allFiles.Clear();

			Queue<string> filenames = new Queue<string>();

			var builder = new InfoBuilder() { FilenameQueue = filenames, ImageList = allFiles, ItemsAvailable = itemsAvailable };
			builder.Start(4);
			foreach (var fm in FolderList)
			{
				var absolutePath = Path.Combine(BasePath, fm.Path);
				try
				{
					FileEnumerator.AddFilenames(
						filenames,
						absolutePath,
						(s) => SupportedExtension(Path.GetExtension(s)));
				}
				catch (Exception ex)
				{
					logger.Info("Exception: {0}", ex);
				}
			}

			builder.WaitForCompletion();
			logger.Info("Enumeration complete");
		}

		bool SupportedExtension(string extension)
		{
			return extension.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
					extension.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
					extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase);
		}
	}

	class InfoBuilder
	{
		public Queue<string> FilenameQueue { get; set; }
		public IList<ImageInformation> ImageList { get; set; }
		public Action ItemsAvailable { get; set; }


		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Task[] _tasks;
		private bool _waiting = false;
		private IDictionary<string,string> _signatures = new ConcurrentDictionary<string, string>();
		private IDictionary<long,FileSignature> _lengthPath = new Dictionary<long, FileSignature>();
		private long _duplicatesRemoved;
		private long _questionableDuplicates;
		private long _primarySignaturesCalculated;
		private long _secondarySignaturesCalculated;


		public void WaitForCompletion()
		{
			_waiting = true;
			Task.WaitAll(_tasks);
			logger.Info(
				"Enumeration completed; {0} duplicates removed; {1} primary calculated; {2} secondary calculated; {3} questionable",
				 _duplicatesRemoved,
				 _primarySignaturesCalculated,
				 _secondarySignaturesCalculated,
				 _questionableDuplicates);
		}

		public void Start(int workers)
		{
			_tasks = new Task[workers];
			for (int idx = 0; idx < workers; ++idx)
			{
				_tasks[idx] = Task.Factory.StartNew( () =>
					{
						try
						{
							while (!_waiting || FilenameQueue.Count > 0)
							{
								string filename = null;
								lock (FilenameQueue)
								{
									if (FilenameQueue.Count > 0)
									{
										filename = FilenameQueue.Dequeue();
									}
								}

								if (null != filename)
								{
									var fileLength = new FileInfo(filename).Length;

									FileSignature prior = null;
									lock(_lengthPath)
									{
										if (_lengthPath.ContainsKey(fileLength))
										{
											prior = _lengthPath[fileLength];
										}
										else
										{
											_lengthPath[fileLength] = new FileSignature { Filename = filename };
										}
									}

									// If there's a possible match, calculate & compare signatures
									if (prior != null)
									{
										if (!prior.HasSignature)
										{
											Interlocked.Increment(ref _primarySignaturesCalculated);
											_signatures[prior.Signature()] = prior.Filename;
										}

										Interlocked.Increment(ref _secondarySignaturesCalculated);
										var fileSignature = new FileSignature { Filename = filename };
										if (_signatures.ContainsKey(fileSignature.Signature()))
										{
											Interlocked.Increment(ref _duplicatesRemoved);

											var priorFile = new FileInfo(_signatures[fileSignature.Signature()]);
											var curFile = new FileInfo(filename);
											if (!curFile.Name.Equals(priorFile.Name) || !curFile.CreationTime.Equals(priorFile.CreationTime))
											{
												Interlocked.Increment(ref _questionableDuplicates);
//												logger.Info("Skipping duplicate: {0} (original = {1})", filename, priorFile.FullName);
											}
											continue;
										}
										else
										{
											_signatures[fileSignature.Signature()] = filename;
										}
									}

									var info = ImageInformation.Get(filename);
									bool wasEmpty = false;
									lock (ImageList)
									{
										wasEmpty = ImageList.Count < 1;
										ImageList.Add(info);
									}

									if (wasEmpty && ItemsAvailable != null)
									{
										logger.Info("Some items available: {0} ...", ImageList.Count);
										ItemsAvailable();
									}
								}
								else
								{
									Thread.Sleep(10);
								}
							}
						}
						catch (Exception ex)
						{
							logger.Error("Exception InfoBuilder: {0}", ex);
						}
					})
					.ContinueWith( (t) => logger.Info("Completed info builder task {0}", t.IsCompleted) );
			}
		}		
	}

	class FileSignature
	{
		public bool HasSignature { get { return _signature != null; } }
		public string Filename { get; set; }
		private string _signature;

		public string Signature()
		{
			lock(this)
			{
				if (_signature == null)
				{
#if true
					// Yes, if a file is less than the data read, we calculate the signature with a bunch
					// of zero byte values. It seems better than re-allocating the byte array to the proper
					// size in that case; the affect is the same, I claim.
					byte[] data = new byte[1024];
					using (var stream = File.OpenRead(Filename))
					{
						stream.Read(data, 0, data.Length);
					}
					_signature = BitConverter.ToString(SHA256.Create().ComputeHash(data)) + new FileInfo(Filename).Length;
#else
					using (var stream = File.OpenRead(Filename))
					{
						_signature = BitConverter.ToString(SHA256.Create().ComputeHash(stream));
					}
#endif
				}
				return _signature;
			}
		}
	}

	public class FolderModel
	{
		public string Path { set; get; }
		public bool Recursive { set; get; }

		static public FolderModel FromElement(XElement element)
		{
			return new FolderModel 
				{
					Path = element.Get("path", null),
					Recursive = element.Get("recursive", false)
				};
		}
	}

	public class Filter
	{
		public bool Exclude { get; set; }
		public string Name { get; set; }

		static public Filter FromElement(XElement element)
		{
			return new Filter
				{
					Exclude = element.Name.ToString().Equals("exclude"),
					Name = element.Get("name", null)
				};
		}
	}

	public enum SlideOrder
	{
		Random,
		DateAscending,
	}
}

