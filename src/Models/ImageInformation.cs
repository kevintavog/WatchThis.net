using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WatchThis.Utilities;

namespace WatchThis.Models
{
	public class ImageInformation
	{
		private Size? _size;

		public Size Size { get { if (_size == null) { _size = ImageDetailsReader.SizeFromFile(this.FullPath); } return _size.Value; } }
		public string Name { get; private set; }
		public string FullPath { get; private set; }
		public DateTime Timestamp { get; private set; }

		private ImageInformation()
		{
		}

		public static ImageInformation Get(string path)
		{
			var info = new ImageInformation { FullPath = path, Name = Path.GetFileName(path), Timestamp = File.GetCreationTime(path) };
			return info;
		}
	}
}

