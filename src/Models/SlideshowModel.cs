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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WatchThis.Models
{
    public class SlideshowModel : INotifyPropertyChanged
	{
		public const string Extension = ".watchthisslideshow";
        public event PropertyChangedEventHandler PropertyChanged;

		public string Filename { set; get; }
		public string Name 
        {
            get { return _name; }
            set { this.SetField(PropertyChanged, ref _name, value, () => Name);  }
        }
		public double SlideSeconds
        { 
            get { return _slideSeconds; }
            set { this.SetField(PropertyChanged, ref _slideSeconds, value, () => SlideSeconds); }
        }
		public double TransitionSeconds { set; get; }
		public bool ManuallyControlled { set; get; }

        private string _name;
        private double _slideSeconds;
        private string _searchQuery;
        private string _findAPhotoHost;
//		public bool ShowOnce { set; get; }
//		public SlideOrder Order { set; get; }

        public string Search
        {
            get { return _searchQuery; }
            set { this.SetField(PropertyChanged, ref _searchQuery, value, () => Search); }
        }

        public string FindAPhotoHost
        {
            get { return _findAPhotoHost; }
            set { this.SetField(PropertyChanged, ref _findAPhotoHost, value, () => FindAPhotoHost); }
        }

        public ObservableCollection<FolderModel> FolderList { get; private set; }

		public List<MediaItem> MediaList { get; set; }
		private static Logger logger = LogManager.GetCurrentClassLogger();


		public SlideshowModel()
		{
            FolderList = new ObservableCollection<FolderModel>();
			MediaList = new List<MediaItem>();
			Reset();
		}

		public void Reset()
		{
//			ShowOnce = true;
//			Order = SlideOrder.Random;
			Filename = null;
			Name = null;
			SlideSeconds = 10;
			TransitionSeconds = 1;
			ManuallyControlled = false;
			FolderList.Clear();
			MediaList.Clear();
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
            ssModel.FindAPhotoHost = doc.Root.GetAttribute(XmlAttrFindAPhotoHost, ssModel.FindAPhotoHost);

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
            foreach (var s in doc.Descendants("search"))
            {
                ssModel.Search = s.GetAttribute("query", null);
            }
            if (ssModel.FolderList.Count < 1 && ssModel.Search == null)
			{
				throw new Exception("Missing either 'folder' or 'search' tag");
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

            if (FindAPhotoHost != null)
            {
                logger.Info("Handle saving host: {0}", FindAPhotoHost);
//                xml.Root.FirstNode.
//                    new XAttribute(XmlAttrFindAPhotoHost, FindAPhotoHost)));
            }

			foreach (var fm in FolderList)
			{
				xml.Root.Add(
					new XElement("folder", 
						new XAttribute("path", fm.Path)));
			}

            if (Search != null)
            {
                xml.Root.Add(
                    new XElement("search",
                        new XAttribute("query", Search)));
            }

			filename = EnsureExtension(filename);
			File.WriteAllText(filename, xml.ToString());
			this.Filename = filename;
		}

		static public string EnsureExtension(string filename)
		{
			if (!filename.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
			{
				filename += Extension;
			}
			return filename;
		}

		public void Enumerate(Action itemsAvailable = null)
		{
			logger.Info("Enumeration started");
            MediaList.Clear();
            MediaCollector collector = new MediaCollector(MediaList) { ItemsAvailable = itemsAvailable } ;
            collector.Start(3);

            var tasks = new List<Task>();
            if (FolderList.Count > 0)
            {
                tasks.Add(Task.Run( () =>
                {
                    var p = new FileProvider(Path.GetDirectoryName(Filename), FolderList);
                    p.Enumerate(collector, itemsAvailable);
                }));
            }

            if (Search != null)
            {
                tasks.Add(Task.Run( () =>
                {
                    var p = new FindAPhotoProvider(FindAPhotoHost, Search);
                    p.Enumerate(collector, itemsAvailable);
                }));
            }

            Task.WaitAll(tasks.ToArray());
            collector.WaitForCompletion();
			logger.Info("Enumeration completed");
		}

		const string XmlRootName = "com.rangic.Slideshow";

        const string XmlEleFolder = "folder";
		const string XmlEleSearch = "search";
		static string[] expectedElements = new []
		{
			XmlEleFolder,
            XmlEleSearch,
		};

		const string XmlAttrName = "name";
		const string XmlAttrSlideDuration = "slideDuration";
		const string XmlAttrTransitionDuration = "transitionDuration";
        const string XmlAttrFindAPhotoHost = "findAPhotoHost";

		static string[] expectedAttributes = new []
		{
			XmlAttrName,
			XmlAttrSlideDuration,
			XmlAttrTransitionDuration,
            XmlAttrFindAPhotoHost,
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

