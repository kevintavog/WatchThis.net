using System;
using System.Net.Http;
using NLog;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace WatchThis.Utilities
{
    static public class LookupOpenStreetMapLocation
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static public string PlaceName(double latitude, double longitude, out string fullResponse)
        {
            fullResponse = null;
            var placeName = "";
            try
            {
                using (var client = new HttpClient())
                {
                    var requestUrl = string.Format(
                        "nominatim/v1/reverse?format=json&lat={0}&lon={1}&addressdetails=1&zoom=18",
                        latitude,
                        longitude);

                    client.BaseAddress = new Uri("http://open.mapquestapi.com/");
                    var result = client.GetAsync(requestUrl).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        // Parse the name out...
                        var body = result.Content.ReadAsStringAsync().Result;
                        fullResponse = body;
                        dynamic response = JObject.Parse(body);
                        if (response["error"] != null)
                        {
                            logger.Warn("GeoLocation error: {0}", response.error);
                        }

                        if (response["address"] != null)
                        {
                            var parts = new List<string>();
                            foreach (var kv in response["address"])
                            {
                                if (AcceptedKeys.Contains(kv.Name))
                                {
                                    parts.Add((string) kv.Value);
                                }
                            }

                            placeName = String.Join(", ", parts);
                        }
                        else
                        if (response["display_name"] != null)
                        {
                            placeName = response.display_name;
                        }
                    }
                    else
                    {
                        logger.Warn("GeoLocation request failed: {0}; {1}; {2}", 
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

            return placeName;
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
            "path",
            "nature_reserve",
            "stadium",
            "state",
            "suburb",
        };
    }
}

