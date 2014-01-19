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
		public double SlideSeconds { set; get; }
		public double TransitionSeconds { set; get; }
		public bool ManuallyControlled { set; get; }

//		public bool ShowOnce { set; get; }
//		public SlideOrder Order { set; get; }

		public IList<FolderModel> FolderList { get; private set; }

		public List<ImageInformation> ImageList { get; set; }
		private static Logger logger = LogManager.GetCurrentClassLogger();


		public SlideshowModel()
		{
//			ShowOnce = true;
//			Order = SlideOrder.Random;
			SlideSeconds = 10;
			TransitionSeconds = 1;
			FolderList = new List<FolderModel>();
			ImageList = new List<ImageInformation>();
		}

		static public SlideshowModel ParseFile(string filename)
		{
			var ssModel = new SlideshowModel();
			var doc = XDocument.Parse(File.ReadAllText(filename));

			if (!doc.Root.Name.LocalName.Equals(XmlRootName))
			{
				throw new Exception(string.Format("Wrong namespace '{0}', expected {1}", doc.Root.Name.LocalName,XmlRootName));
			}

			ssModel.Filename = filename;
			ssModel.Name = doc.Root.GetAttribute(XmlAttrName, "");
			ssModel.SlideSeconds = doc.Root.GetAttribute(XmlAttrSlideDuration, ssModel.SlideSeconds);
			ssModel.TransitionSeconds = doc.Root.GetAttribute(XmlAttrTransitionDuration, ssModel.TransitionSeconds);

			foreach (var a in doc.Root.Attributes())
			{
				if (!expectedAttributes.Contains(a.Name.LocalName))
				{
					throw new Exception(string.Format("Unexpected attribute: {0}", a.Name.LocalName));
				}
			}

			foreach (var f in doc.Descendants("folder"))
			{
				ssModel.FolderList.Add(FolderModel.FromElement(f));
			}
			if (ssModel.FolderList.Count < 1)
			{
				throw new Exception("Missing 'folder' tag");
			}

			foreach (var e in doc.Root.Elements())
			{
				if (!expectedElements.Contains(e.Name.LocalName))
				{
					throw new Exception(string.Format("Unexpected element: {0}", e.Name.LocalName));
				}
			}

			return ssModel;
		}

		public void Save(string filename)
		{
			var xml = new XDocument(
				new XElement(XmlRootName,
					new XAttribute(XmlAttrName, Name),
					new XAttribute(XmlAttrSlideDuration, SlideSeconds),
					new XAttribute(XmlAttrTransitionDuration, TransitionSeconds)));

			foreach (var fm in FolderList)
			{
				xml.Root.Add(
					new XElement("folder", 
						new XAttribute("path", fm.Path)));
			}

			if (!filename.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
			{
				filename += Extension;
			}
			File.WriteAllText(filename, xml.ToString());
		}

		public void Enumerate(Action itemsAvailable = null)
		{
			logger.Info("Enumeration started");
			List<ImageInformation> allFiles = (List<ImageInformation>)ImageList;
			allFiles.Clear();

			Queue<string> filenames = new Queue<string>();

			var relativePath = Path.GetDirectoryName(Filename);
			var builder = new InfoBuilder() { FilenameQueue = filenames, ImageList = allFiles, ItemsAvailable = itemsAvailable };
			builder.Start(3);
			foreach (var fm in FolderList)
			{
				var path = Path.Combine(fm.Path);
				if (!Path.IsPathRooted(path))
				{
					path = Path.GetFullPath(Path.Combine(relativePath, path));
				}

				if (Directory.Exists(path))
				{
					try
					{
						FileEnumerator.AddFilenames(
							filenames,
							path,
							(s) => SupportedExtension(Path.GetExtension(s)));
					}
					catch (Exception ex)
					{
						logger.Info("Exception: {0}", ex);
					}
				}
				else
				{
					logger.Warn("Ignoring non-existent path: '{0}'",  path);
				}
			}

			builder.WaitForCompletion();
			logger.Info("Enumeration completed");
		}

		bool SupportedExtension(string extension)
		{
			return extension.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
					extension.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
					extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase);
		}

		const string XmlRootName = "com.rangic.Slideshow";

		const string XmlEleFolder = "folder";
		static string[] expectedElements = new []
		{
			XmlEleFolder,
		};

		const string XmlAttrName = "name";
		const string XmlAttrSlideDuration = "slideDuration";
		const string XmlAttrTransitionDuration = "transitionDuration";

		static string[] expectedAttributes = new []
		{
			XmlAttrName,
			XmlAttrSlideDuration,
			XmlAttrTransitionDuration
		};
	}

#if false
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
#endif
	public class FolderModel
	{
		public string Path { set; get; }
		public bool Recursive { set; get; }

		static public FolderModel FromElement(XElement element)
		{
			return new FolderModel 
				{
					Path = element.GetAttribute("path", null),
					Recursive = element.GetAttribute("recursive", true)
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
					Name = element.GetAttribute("name", null)
				};
		}
	}

	public enum SlideOrder
	{
		Random
	}
}

