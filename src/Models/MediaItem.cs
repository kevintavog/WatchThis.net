using System;
using WatchThis.Utilities;
using System.IO;
using Rangic.Utilities.Geo;

namespace WatchThis.Models
{
    public class MediaItem
    {
        public IProvider Provider { get; private set; }
        public object Data { get; private set; }

        public bool HasSignature { get { return _signature != null; } }

        private string _signature;


        public MediaItem(IProvider provider, object data)
        {
            Data = data;
            Provider = provider;
        }

        public string ParentDirectoryName
        {
            get
            {
                return Provider.ParentDirectoryName(Data);
            }
        }

        public long Length
        {
            get
            {
                return Provider.Length(Data);
            }
        }

        public DateTime CreatedDate
        {
            get
            {
                return Provider.CreatedDate(Data);
            }
        }

        public string Identifier
        {
            get
            {
                return Provider.Identifier(Data);
            }
        }

        public string Signature
        {
            get
            {
                if (!HasSignature)
                {
                    _signature = Provider.Signature(Data);
                }
                return _signature;
            }
        }

        public Location GetLocation()
        {
            return Provider.GetLocation(Data);
        }

        public Stream Stream()
        {
            return Provider.Stream(Data);
        }
    }
}

