using System;
using NLog;
using System.Collections.Generic;
using WatchThis.Utilities;

namespace WatchThis.Utilities
{
    public class Location
    {
        public override string ToString()
        {
            return string.Format("[Location: Latitude={0}, Longitude={1}, PlaceName={2}]", Latitude, Longitude, PlaceName);
        }

        public string FullResponse { get; private set; }

        private string _placeName;
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string PlaceName
        {
            get 
            {
                if (_placeName == null && Latitude != 0.0F && Longitude != 0.0F)
                {
                    string fullResponse;
                    _placeName = LookupOpenStreetMapLocation.PlaceName(Latitude, Longitude, out fullResponse);
                    FullResponse = fullResponse;
                }
                return _placeName;
            }
        }

        public Location(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
