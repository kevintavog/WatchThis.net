using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using NLog;
using WatchThis.Utilities;
using Newtonsoft.Json.Linq;

namespace WatchThis.Utilities
{
    public class Location
    {
        public enum PlaceNameFilter
        {
            None,
            Minimal,
            Standard
        }

        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        static public Location None = new Location(1000, 1000);
        static public bool IsNone(Location loc)
        {
            return loc == Location.None;
        }

        static public bool IsNullOrNone(Location loc)
        {
            return loc == null || loc == Location.None;
        }

        public Location(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return string.Format("[Location: Latitude={0}, Longitude={1}]", Latitude, Longitude);
        }

        public string ToDms()
        {
            if (this == None)
            {
                return "";
            }

            char latNS = Latitude < 0 ? 'S' : 'N';
            char longEW = Longitude < 0 ? 'W' : 'E';
            return String.Format("{0} {1}, {2} {3}", ToDms(Latitude), latNS, ToDms(Longitude), longEW);
        }

        private string ToDms(double l)
        {
            if (l < 0)
            {
                l *= -1f;
            }
            var degrees = Math.Truncate(l);
            var minutes = (l - degrees) * 60f;
            var seconds = (minutes - (int) minutes) * 60;
            minutes = Math.Truncate(minutes);
            return String.Format("{0:00}° {1:00}' {2:00}\"", degrees, minutes, seconds);
        }

        public string PlaceName(PlaceNameFilter filter)
        {
            var parts = new List<string>();
            foreach (var key in PlaceNameComponents.Keys)
            {
                switch (filter)
                {
                    case PlaceNameFilter.Standard:
                        if (AcceptedKeys.Contains((string) key))
                        {
                            parts.Add((string) PlaceNameComponents[key]);
                        }
                        if ("county" == (string) key && !PlaceNameComponents.Contains("city"))
                        {
                            parts.Add((string) PlaceNameComponents[key]);
                        }
                        break;

                    case PlaceNameFilter.None:
                        if ("DisplayName" != (string) key)
                            parts.Add((string) PlaceNameComponents[key]);
                        break;

                    case PlaceNameFilter.Minimal:
                        if ("country_code" != (string) key)
                        {
                            if ("county" == (string) key)
                            {
                                if (!PlaceNameComponents.Contains("city"))
                                {
                                    parts.Add((string) PlaceNameComponents[key]);
                                }
                            }
                            else
                            {
                                parts.Add((string) PlaceNameComponents[key]);
                            }
                        }
                        break;
                }
            }

            if (!parts.Any() && PlaceNameComponents.Contains("DisplayName"))
                parts.Add((string) PlaceNameComponents["DisplayName"]);


            return String.Join(", ", parts);
        }

        private OrderedDictionary placeNameComponents;
        public OrderedDictionary PlaceNameComponents
        {
            get
            {
                if (placeNameComponents == null)
                {
                    placeNameComponents = RetrievePlaceNameComponents();
                }

                return placeNameComponents;
            }
        }

        private OrderedDictionary RetrievePlaceNameComponents()
        {
            var pnc = new OrderedDictionary();

            try
            {
                using (var client = new HttpClient())
                {
                    var requestUrl = string.Format(
                        "nominatim/v1/reverse?format=json&lat={0}&lon={1}&addressdetails=1&zoom=18&accept-language=en-us",
                        Latitude,
                        Longitude);

                    client.BaseAddress = new Uri("http://open.mapquestapi.com/");
                    var result = client.GetAsync(requestUrl).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        // Parse the name out...
                        var body = result.Content.ReadAsStringAsync().Result;
                        dynamic response = JObject.Parse(body);
                        if (response["error"] != null)
                        {
                            logger.Warn("GeoLocation error: {0}", response.error);
                        }

                        if (response["display_name"] != null)
                        {
                            pnc.Add("DisplayName", (string) response.display_name);
                        }

                        if (response["address"] != null)
                        {
                            foreach (var kv in response["address"])
                            {
                                pnc.Add(kv.Name, (string) kv.Value);
                            }
                        }
                    }
                    else
                    {
                        logger.Warn("GeoLocation request failed: {0}; {1}; {1}", 
                            result.StatusCode, 
                            result.ReasonPhrase,
                            result.Content.ReadAsStringAsync().Result);
                    }
                }
            }
            catch (AggregateException ae)
            {
                logger.Warn("Exception getting geolocation:");
                foreach (var inner in ae.InnerExceptions)
                {
                    logger.Warn("  {0}", inner.Message);
                }
            }
            catch (Exception e)
            {
                logger.Warn("Exception getting geolocation: {0}", e);
            }

            return pnc;
        }

        // This really ought to be configurable by the user. Even better if they could pick from the tags
        // found in their image set
        static private ISet<string> AcceptedKeys = new HashSet<string>
        {
            "attraction",
            "basin",
            "city",
            "country",
            "cycleway",
            "footway",
            "garden",
            "hamlet",
            "park",
            "path",
            "nature_reserve",
            "stadium",
            "state",
            "suburb",
        };
    }
}
