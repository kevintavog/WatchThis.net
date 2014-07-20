using System;
using System.Collections.Generic;
using NLog;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using WatchThis.Utilities;
using System.IO;
using System.Web;

namespace WatchThis.Models
{
    public class FindAPhotoProvider : IProvider
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string host;
        private string search;


        public FindAPhotoProvider(string host, string search)
        {
            this.search = search;
            this.host = host;
        }

        public void Enumerate(MediaCollector collector, Action itemsAvailable = null)
        {
            using (var client = new HttpClient())
            {
                int first = 0;
                int count = 100;
                client.BaseAddress = new Uri(host);

                try
                {
                    bool searchAgain = true;
                    do
                    {
                        searchAgain = false;
                        var requestUrl = string.Format("api/search?f={0}&c={1}&q={2}", first, count, search);
                        var result = client.GetAsync(requestUrl).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            var body = result.Content.ReadAsStringAsync().Result;
                            dynamic response = JObject.Parse(body);

                            if (response["matches"] != null)
                            {
                                first += count;
                                foreach (var m in response["matches"])
                                {
                                    searchAgain = true;
                                    var mimeType = m["mimeType"].ToString();
                                    if (mimeType != null && mimeType.StartsWith("image"))
                                    {
                                        var item = new ItemCache
                                        {
                                            Url = m["fullUrl"],
                                            Length = m["length"],
                                            MimeType= m["mimeType"],
                                            CreatedDate = m["createdDate"],
                                            Signature = m["signature"],
                                            Latitude = m["latitude"],
                                            Longitude = m["longitude"],
                                        };

                                        logger.Info("Lat/long {0}, {1}", item.Latitude, item.Longitude);
                                        collector.AddItem(new MediaItem(this, item));
                                    }
                                }
                            }
                        }
                        else
                        {
                            logger.Error(
                                "FindAPhoto search failed: {0}; {1}; {2}", 
                                result.StatusCode, 
                                result.ReasonPhrase, 
                                result.Content.ReadAsStringAsync().Result);
                            break;
                        }
                    } while (searchAgain);
                }
                catch (Exception ex)
                {
                    logger.Error("Exception searching: {0}", ex);
                }
            }
        }

        public long Length(object data)
        {
            return (data as ItemCache).Length;
        }

        public DateTime CreatedDate(object data)
        {
            return (data as ItemCache).CreatedDate;
        }

        public string Signature(object data)
        {
            return (data as ItemCache).Signature;
        }

        public string Identifier(object data)
        {
            return (data as ItemCache).Url;
        }

        public Location GetLocation(object data)
        {
            var item = data as ItemCache;
            var l = new Location(item.Latitude, item.Longitude);

            logger.Info("Location: {0}, [{1}]", l, l.FullResponse);

            return l;
        }

        public Stream Stream(object data)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(host);
                return client.GetStreamAsync(new Uri(client.BaseAddress, (data as ItemCache).Url)).Result;
            }
        }

        public string ParentDirectoryName(object data)
        {
            var path = "/" + HttpUtility.UrlDecode((data as ItemCache).Url);
            return Path.GetDirectoryName(path);
        }
    }

    class ItemCache
    {
        public string Url { get; set; }
        public long Length { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Signature { get; set; }
        public string MimeType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
