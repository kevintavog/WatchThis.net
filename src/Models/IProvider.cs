using System;
using WatchThis.Utilities;
using System.IO;

namespace WatchThis.Models
{
    public interface IProvider
    {
        void Enumerate(MediaCollector collector, Action itemsAvailable = null);
        long Length(object data);
        DateTime CreatedDate(object data);
        string Signature(object data);
        string Identifier(object data);
        Location GetLocation(object data);
        Stream Stream(object data);
        string ParentDirectoryName(object data);
    }
}

