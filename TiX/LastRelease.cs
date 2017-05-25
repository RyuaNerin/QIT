using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace TiX
{
    internal static class LastRelease
    {
        public class LastestRealease
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; }

            [JsonProperty("assets")]
            public Asset[] Assets { get; set; }

            [JsonObject]
            public class Asset
            {
                [JsonProperty("browser_download_url")]
                public string BrowserDownloadUrl { get; set; }
            }
        }

        public static LastestRealease CheckNewVersion()
        {
            try
            {
                LastestRealease last;

                var req = HttpWebRequest.Create("https://api.github.com/repos/RyuaNerin/QIT/releases/latest") as HttpWebRequest;
                req.Timeout = 5000;
                req.UserAgent = "QIT";
                using (var res = req.GetResponse())
                {
                    var json = new JsonSerializer();

                    using (var rStream = res.GetResponseStream())
                    using (var sReader = new StreamReader(rStream))
                    using (var jReader = new JsonTextReader(sReader))
                    {
                        last = json.Deserialize<LastestRealease>(jReader);
                    }
                }

                return new Version(last.TagName) > new Version(Application.ProductVersion) ? last : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
