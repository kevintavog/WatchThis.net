using System;
using NLog;
using System.Collections.Generic;
using System.IO;
using WatchThis.Utilities;
using Rangic.Utilities.Geo;
using Rangic.Utilities.Image;

namespace WatchThis.Models
{
    public class FileProvider : IProvider
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string baseFolder;
        private IList<FolderModel> folderList;

        public FileProvider(string baseFolder, IList<FolderModel> folderList)
        {
            this.baseFolder = baseFolder;
            this.folderList = folderList;
        }

        public void Enumerate(MediaCollector collector, Action itemsAvailable = null)
        {
            foreach (var fm in folderList)
            {
                var path = Path.Combine(fm.Path);
                if (!Path.IsPathRooted(path))
                {
                    path = Path.GetFullPath(Path.Combine(baseFolder, path));
                }

                if (Directory.Exists(path))
                {
                    try
                    {
                        FileEnumerator.AddFilenames(
                            (s) => collector.AddItem(new MediaItem(this, s)),
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
        }

        public DateTime CreatedDate(object data)
        {
            return new FileInfo(data as string).CreationTime;
        }

        public string Signature(object data)
        {
            return new FileSignature { Filename = data as string }.Signature();
        }

        public string Identifier(object data)
        {
            return data as string;
        }

        public long Length(object data)
        {
            return new FileInfo(data as String).Length;
        }

        public Location GetLocation(object data)
        {
            return new ImageDetails(data as String).Location;
        }

        public Stream Stream(object data)
        {
            return new FileStream(data as string, FileMode.Open, FileAccess.Read);
        }

        public string ParentDirectoryName(object data)
        {
            return Path.GetDirectoryName(data as string);
        }

        bool SupportedExtension(string extension)
        {
            return extension.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                extension.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
