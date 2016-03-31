using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TiX.Utilities
{
    internal static class Http
    {        
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36";

        private readonly static CookieContainer Cookies = new CookieContainer();
        private readonly static Jint.Engine JSEngine = new Jint.Engine();
        public static bool GetResponse(Uri uri, Stream stream, ParallelLoopState state, bool passException = false)
        {
            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.Referer = new Uri(uri, "/").ToString();
            req.UserAgent = UserAgent;
            req.Headers.Set("cookie", Cookies.GetCookieHeader(uri));

            HttpWebResponse res;
            try
            {
                using (res = req.GetResponse() as HttpWebResponse)
                {
                    if (res.ContentType.IndexOf("image", StringComparison.CurrentCultureIgnoreCase) == -1)
                        return false;

                    return GetResponseBytes(res, stream, state);
                }
            }
            catch (WebException e)
            {
                if (passException)
                {
                    e.Response.Close();
                    return false;
                }

                string body;
                string cookies;
                string refresh;
                using (res = e.Response as HttpWebResponse)
                {
                    if (res.Server.IndexOf("cloudflare", StringComparison.CurrentCultureIgnoreCase) == -1)
                        return false;

                    using (var mem = new MemoryStream())
                    {
                        if (!GetResponseBytes(res, mem, state)) return false;

                        mem.Position = 0;
                        using (var reader = new StreamReader(mem))
                            body = reader.ReadToEnd();
                    }

                    cookies = res.Headers["set-cookie"];
                    refresh = res.Headers["refresh"];
                }

                return BypassCloudFlare(body, cookies, refresh, uri, stream, state);
            }
            catch
            {
            }

            return false;
        }

        private static bool GetResponseBytes(HttpWebResponse res, Stream stream, ParallelLoopState state)
        {
            using (var http = res.GetResponseStream())
            {
                int rd;
                var buff = new byte[40960]; // 40k

                while (!state.IsStopped && !state.ShouldExitCurrentIteration && (rd = http.Read(buff, 0, 40960)) > 0)
                    stream.Write(buff, 0, rd);
            }

            return !state.IsStopped && !state.ShouldExitCurrentIteration;
        }

        private readonly static RegexOptions RegexDefaultOption = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        private readonly static Regex regCF_challenge       = new Regex("name=\"jschl_vc\" value=\"(\\w+)\"", RegexDefaultOption);
        private readonly static Regex regCF_challengepass   = new Regex("name=\"pass\" value=\"(.+?)\"", RegexDefaultOption);
        private readonly static Regex regCG_script          = new Regex(@"setTimeout\(function\(\){\s+(var t,r,a,f.+?\r?\n[\s\S]+?a\.value =.+?)\r?\n", RegexDefaultOption);
        private readonly static Regex regCG_scriptReplace0  = new Regex(@"a\.value =(.+?) \+ .+?;", RegexDefaultOption);
        private readonly static Regex regCG_scriptReplace1  = new Regex(@"\s{3,}[a-z](?: = |\.).+", RegexDefaultOption);
        private readonly static Regex regCG_scriptReplace2  = new Regex(@"[\n\\']", RegexDefaultOption);
        //private readonly static Regex regRefresh  = new Regex(@"(\d+); *url=([^;]+)", RegexDefaultOption);
        private static bool BypassCloudFlare(string body, string setCookieHeader, string refreshHeader, Uri uri, Stream stream, ParallelLoopState state)
        {
            // http://stackoverflow.com/questions/32425973/how-can-i-get-html-from-page-with-cloudflare-ddos-portection#2

            // 기존 쿠키 추가
            Cookies.Add(StringToCookies(setCookieHeader, uri.Host));

            Uri newUri;

            /*
            if (string.IsNullOrWhiteSpace(refreshHeader))
            {
            */
            var challenge = regCF_challenge.Match(body).Groups[1].Value;
            var challenge_pass = regCF_challengepass.Match(body).Groups[1].Value;

            var builder = regCG_script.Match(body).Groups[1].Value;
            builder = regCG_scriptReplace0.Replace(builder, "$1");
            builder = regCG_scriptReplace1.Replace(builder, "");
            builder = regCG_scriptReplace2.Replace(builder, "");
             
            var solved = Convert.ToInt64(JSEngine.Execute(builder).GetCompletionValue().ToObject());
            solved += uri.Host.Length;

            if (!Wait(3, state))
                return false;

            var cookie_url = string.Format("{0}://{1}/cdn-cgi/l/chk_jschl", uri.Scheme, uri.Host);
            var uri_builder = new UriBuilder(cookie_url);

            var query = new Dictionary<string, object>();
            query["jschl_vc"] = challenge;
            query["pass"] = challenge_pass;
            query["jschl_answer"] = solved;
            uri_builder.Query = ToString(query);

            newUri = uri_builder.Uri;
            /*
            }
            else
            {
                var m = regRefresh.Match(refreshHeader);

                if (!Wait(int.Parse(m.Groups[1].Value), state))
                    return false;

                newUri = new Uri(uri, m.Groups[2].Value);
            }
            */

            var req = WebRequest.Create(newUri) as HttpWebRequest;
            req.AllowAutoRedirect = false;
            req.Headers.Set("cookie", Cookies.GetCookieHeader(uri));
            req.Referer = uri.ToString();
            req.UserAgent = UserAgent;

            using (var res = req.GetResponse() as HttpWebResponse)
            {
                setCookieHeader = res.Headers["Set-Cookie"];
                if (string.IsNullOrWhiteSpace(setCookieHeader))
                    return false;
                
                Cookies.Add(StringToCookies(res.Headers["Set-Cookie"], uri.Host));
            }

            return GetResponse(uri, stream, state, true);
        }

        private static bool Wait(int seconds, ParallelLoopState state)
        {
            var wait = DateTime.UtcNow.AddSeconds(seconds);
            do
            {
                Thread.Sleep(250);
            } while (DateTime.UtcNow < wait && !state.IsStopped && !state.ShouldExitCurrentIteration);
            
            return !state.IsStopped && !state.ShouldExitCurrentIteration;
        }

        private static CookieCollection StringToCookies(string setCookies, string domain)
        {
            var cc = new CookieCollection();
            if (string.IsNullOrWhiteSpace(setCookies))
                return cc;

            int i, k;
            string[] kvs;
            string key, val;

            foreach (var str in SplitEachCookie(setCookies))
            {
                var cookie = new Cookie();
                cookie.Domain = domain;
                cookie.Path = "/";

                kvs = str.Split(';');
                for (i = 0; i < kvs.Length; ++i)
                {
                    k   = kvs[i].IndexOf('=');
                    if (k == -1)
                    {
                        key = kvs[i];
                        val = null;
                    }
                    else
                    {
                        key = kvs[i].Substring(0, k);
                        val = kvs[i].Substring(k + 1);
                    }

                    if (i == 0)
                    {
                        cookie.Name = key;
                        cookie.Value = val;
                    }
                    else
                    {
                        key = key.ToLower();
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            switch (key)
                            {
                            case "httponly":
                                cookie.HttpOnly = true;
                                break;
                            case "discard":
                                cookie.Discard = true;
                                break;
                            case "secure":
                                cookie.Secure = true;
                                break;
                            }
                        }
                        else
                        {
                            switch (key)
                            {
                            case "expires":
                                if (cookie.Expires == DateTime.MinValue)
                                    cookie.Expires = ParseCookieExpires(val);
                                break;

                            case "max-age":
                                if (cookie.Expires == DateTime.MinValue)
                                    cookie.Expires = cookie.TimeStamp.AddSeconds(int.Parse(val));
                                break;

                            case "path":
                                cookie.Path = val;
                                break;

                            case "domain":
                                cookie.Domain = val;
                                break;

                            case "port":
                                cookie.Port = val;
                                break;

                            case "comment":
                                cookie.Comment = val;
                                break;

                            case "commenturl":
                                try
                                {
                                    cookie.CommentUri = new Uri(val);
                                }
                                catch { }
                                break;

                            case "version":
                                try
                                {
                                    cookie.Version = int.Parse(val);
                                }
                                catch { }
                                break;

                            }
                        }
                    }
                }

                cc.Add(cookie);
            }
            return cc;
        }

        private readonly static string[] cookieExpiresFormats =
        {
            "r",
            "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'",
            "ddd, dd'-'MMM'-'yy HH':'mm':'ss 'GMT'",
            "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'UTC'",
            "ddd, dd'-'MMM'-'yy HH':'mm':'ss 'UTC'"
        };

        private static IEnumerable<string> SplitEachCookie(string setCookies)
        {
            var lst = new List<string>();

            int read = 0;
            int find = 0;

            while (read < setCookies.Length)
            {
                find = setCookies.IndexOf(',', read + 1);
                if (find != -1 && setCookies.IndexOf("expires=", read, find - read, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    find = setCookies.IndexOf(',', find + 1);

                if (find == -1)
                    find = setCookies.Length;

                yield return setCookies.Substring(read, find - read);

                read = find + 1;
            }
        }
        private static DateTime ParseCookieExpires(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            DateTime date;

            for (int i = 0; i < cookieExpiresFormats.Length; i++)
            {
                if (DateTime.TryParseExact(value, cookieExpiresFormats[i], CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                {
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    return TimeZone.CurrentTimeZone.ToLocalTime(date);
                }
            }

            return DateTime.MinValue;
        }

        private static string ToString(IDictionary<string, object> dic)
        {
            if (dic == null) return null;

            var sb = new StringBuilder();

            if (dic.Count > 0)
            {
                foreach (var st in dic)
                    if (st.Value is bool)
                        sb.AppendFormat("{0}={1}&", st.Key, (bool)st.Value ? "true" : "false");
                    else
                        sb.AppendFormat("{0}={1}&", st.Key, Uri.EscapeUriString(Convert.ToString(st.Value)));

                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }
    }
}
