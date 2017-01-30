// By RyuaNerin
// 트위터 라이브러리 만들던것에서 빼옴

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Limitation
{
    internal class OAuth
    {
        static OAuth()
        {
            ServicePointManager.Expect100Continue = false;
        }

        public OAuth(string appToken, string appSecret)
            : this(appToken, appSecret, null, null)
        {
        }
        public OAuth(string appToken, string appSecret, string userToken, string userSecret)
        {
            this.AppToken   = appToken;
            this.AppSecret  = appSecret;
            this.UserToken  = userToken;
            this.UserSecret = userSecret;
        }

        public string AppToken      { get; private set; }
        public string AppSecret     { get; private set; }
        public string UserToken     { get; set; }
        public string UserSecret    { get; set; }

        private static string[] oauth_array = { "oauth_consumer_key", "oauth_version", "oauth_nonce", "oauth_signature", "oauth_signature_method", "oauth_timestamp", "oauth_token", "oauth_callback" };

        public WebRequest CreateWebRequest(string method, string url, object data = null)
        {
            method = method.ToUpper();
            var uri = new Uri(url);
            var dic = new SortedDictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(uri.Query))
                OAuth.AddDictionary(dic, uri.Query);

            if (data != null)
                OAuth.AddDictionary(dic, data);

            if (!string.IsNullOrWhiteSpace(this.UserToken))
                dic.Add("oauth_token", this.UserToken);

            dic.Add("oauth_consumer_key", this.AppToken);
            dic.Add("oauth_nonce", OAuth.GetNonce());
            dic.Add("oauth_timestamp", OAuth.GetTimeStamp());
            dic.Add("oauth_signature_method", "HMAC-SHA1");
            dic.Add("oauth_version", "1.0");

            var hashKey = string.Format(
                "{0}&{1}",
                UrlEncode(this.AppSecret),
                string.IsNullOrWhiteSpace(this.UserSecret) ? null : UrlEncode(this.UserSecret));
            var hashData = string.Format(
                    "{0}&{1}&{2}",
                    method.ToUpper(),
                    UrlEncode(string.Format("{0}{1}{2}{3}", uri.Scheme, Uri.SchemeDelimiter, uri.Host, uri.AbsolutePath)),
                    UrlEncode(OAuth.ToString(dic)));

            using (var hash = new HMACSHA1(Encoding.UTF8.GetBytes(hashKey)))
                dic.Add("oauth_signature", UrlEncode(Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(hashData)))));

            var sbData = new StringBuilder();
            sbData.Append("OAuth ");
            foreach (var st in dic)
                if (Array.IndexOf<string>(oauth_array, st.Key) >= 0)
                    sbData.AppendFormat("{0}=\"{1}\",", st.Key, Convert.ToString(st.Value));
            sbData.Remove(sbData.Length - 1, 1);

            var str = sbData.ToString();

            var req = (HttpWebRequest)WebRequest.Create(uri);
            req.Method = method;
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            req.UserAgent = "Limitation";
            req.Headers.Add("Authorization", sbData.ToString());

            if (method == "POST")
                req.ContentType = "application/x-www-form-urlencoded";

            return req;
        }

        private static string GetNonce()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static DateTime GenerateTimeStampDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private static string GetTimeStamp()
        {
            return Convert.ToInt64((DateTime.UtcNow - GenerateTimeStampDateTime).TotalSeconds).ToString();
        }

        private static string UrlEncode(string str)
        {
            var uriData = Uri.EscapeDataString(str);
            var sb = new StringBuilder(uriData.Length);
            
            for (int i = 0; i < uriData.Length; ++i)
            {
                switch (uriData[i])
                {
                case '!':  sb.Append("%21"); break;
                case '*':  sb.Append("%2A"); break;
                case '\'': sb.Append("%5C"); break;
                case '(':  sb.Append("%28"); break;
                case ')':  sb.Append("%29"); break;
                default:   sb.Append(uriData[i]); break;
                }
            }

            return sb.ToString();
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
                        sb.AppendFormat("{0}={1}&", st.Key, Convert.ToString(st.Value));

                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        public static string ToString(object values)
        {
            if (values == null) return null;

            var sb = new StringBuilder();

            string name;
            object value;

            foreach (var p in values.GetType().GetProperties())
            {
                if (!p.CanRead) continue;

                name  = p.Name;
                value = p.GetValue(values, null);

                if (value is bool)
                    sb.AppendFormat("{0}={1}&", name, (bool)value ? "true" : "false");
                else
                    sb.AppendFormat("{0}={1}&", name, UrlEncode(Convert.ToString(value)));
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        private static void AddDictionary(IDictionary<string, object> dic, string query)
        {
            if (!string.IsNullOrEmpty(query) || (query.Length > 1))
            {
                int read = 0;
                int find = 0;

                if (query[0] == '?')
                    read = 1;

                string key, val;

                while (read < query.Length)
                {
                    find = query.IndexOf('=', read);
                    key = query.Substring(read, find - read);
                    read = find + 1;

                    find = query.IndexOf('&', read);
                    if (find > 0)
                    {
                        if (find - read == 1)
                            val = null;
                        else
                            val = query.Substring(read, find - read);

                        read = find + 1;
                    }
                    else
                    {
                        val = query.Substring(read);

                        read = query.Length;
                    }

                    dic[key] = val;
                }
            }
        }

        private static void AddDictionary(IDictionary<string, object> dic, object values)
        {
            object value;

            foreach (var p in values.GetType().GetProperties())
            {
                if (!p.CanRead) continue;
                value = p.GetValue(values, null);

                if (value is bool)
                    dic[p.Name] = (bool)value ? "true" : "false";
                else
                    dic[p.Name] = UrlEncode(Convert.ToString(value));


            }
        }

        public bool RequestToken(out string token, out string secret)
        {
            try
            {
                var req = CreateWebRequest("POST", "https://api.twitter.com/oauth/request_token");
                using (var res = req.GetResponse())
                using (var reader = new StreamReader(res.GetResponseStream()))
                {
                    var str = reader.ReadToEnd();

                    token  = Regex.Match(str, @"oauth_token=([^&]+)").Groups[1].Value;
                    secret = Regex.Match(str, @"oauth_token_secret=([^&]+)").Groups[1].Value;

                    return true;
                }
            }
            catch
            {
            }

            token = secret = null;
            return false;
        }

        public bool AccessToken(string verifier, out string token, out string secret)
        {
            try
            {
                var obj = new { oauth_verifier = verifier };
                var buff = Encoding.UTF8.GetBytes(OAuth.ToString(obj));

                var req = CreateWebRequest("POST", "https://api.twitter.com/oauth/access_token", obj);
                req.ContentLength = buff.Length;
                req.GetRequestStream().Write(buff, 0, buff.Length);

                using (var res = req.GetResponse())
                using (var reader = new StreamReader(res.GetResponseStream()))
                {
                    var str = reader.ReadToEnd();
                    
                    token  = Regex.Match(str, @"oauth_token=([^&]+)").Groups[1].Value;
                    secret = Regex.Match(str, @"oauth_token_secret=([^&]+)").Groups[1].Value;

                    return true;
                }
            }
            catch
            {
            }

            token = secret = null;
            return false;
        }
    }
}
