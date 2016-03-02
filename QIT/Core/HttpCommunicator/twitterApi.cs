using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace Twitter
{
	public class TwitterAPI : IDisposable
	{
		public void Dispose()
		{
			this.dic.Clear();
		}
		public TwitterAPI(string token, string secret)
		{
			this.ConsumerToken = token;
			this.ConsumerSecret = secret;
			this.CallBackURL = null;
		}
		public TwitterAPI(string token, string secret, string utoken, string usecret)
		{
			this.ConsumerToken = token;
			this.ConsumerSecret = secret;
			this.UserToken = utoken;
			this.UserSecret = usecret;
			this.CallBackURL = null;
		}

		SortedDictionary<string, string> dic = new SortedDictionary<string, string>();

		public string UserToken { get; set; }
		public string UserSecret { get; set; }
		public string ConsumerToken { get; set; }
		public string ConsumerSecret { get; set; }
		public string CallBackURL { get; set; }

		public string this[string key]
		{
			get
			{
				if (dic.ContainsKey(key))
					return dic[key];
				else
					return null;
			}
			set
			{
				if (dic.ContainsKey(key))
					dic[key] = value;
				else
					dic.Add(key, value);
			}
		}

		public void Clear()
		{
			this.dic.Clear();
			this.CallBackURL = null;
		}

		private static string[] oauth_array = new string[] { "oauth_consumer_key", "oauth_version", "oauth_nonce", "oauth_signature", "oauth_signature_method", "oauth_timestamp", "oauth_token", "oauth_callback" };
		public string GetResponsePOST(string url)
		{
			return GetResponse(url, true, false);
		}
		public string GetResponsePOST2(string url)
		{
			return GetResponse(url, true, true);
		}
		public string GetResponseGET(string url)
		{
			return GetResponse(url, false, false);
		}
		private string GetResponse(string URL, bool isPost, bool GetErrorMsg)
		{
			string responseBody;

			try
			{
				string oauth_nonce, oauth_timestamp, hash_parameter, oauth_signature, hash_key, http_method;

				http_method = isPost ? "POST" : "GET";
				oauth_nonce = TwitterAPI.GetNonce();
				oauth_timestamp = TwitterAPI.GenerateTimeStamp();

				// 이것저것 추가
				if (this.CallBackURL != null)
					this.dic.Add("oauth_callback", this.CallBackURL);

				if (this.UserToken != null)
					this.dic.Add("oauth_token", this.UserToken);

				this.dic.Add("oauth_consumer_key", ConsumerToken);
				this.dic.Add("oauth_nonce", oauth_nonce);
				this.dic.Add("oauth_timestamp", oauth_timestamp);
				this.dic.Add("oauth_signature_method", "HMAC-SHA1");
				this.dic.Add("oauth_version", "1.0");

				// hash parameter 를 만든다
				StringBuilder sbPost = new StringBuilder();
				foreach (KeyValuePair<string, string> st in this.dic)
					sbPost.AppendFormat("{0}={1}&", st.Key, TwitterAPI.UrlEncode(st.Value));
				if (sbPost.Length > 0)
					sbPost.Remove(sbPost.Length - 1, 1);
				hash_parameter = sbPost.ToString();

				sbPost.Remove(0, sbPost.Length);
				foreach (KeyValuePair<string, string> st in this.dic)
					if (Array.IndexOf<string>(oauth_array, st.Key) < 0)
						sbPost.AppendFormat("{0}={1}&", st.Key, TwitterAPI.UrlEncode(st.Value));
				if (sbPost.Length > 0)
					sbPost.Remove(sbPost.Length - 1, 1);

				// hash key 만듬
				if (this.UserToken != null)
					hash_key = string.Format("{0}&{1}", TwitterAPI.UrlEncode(ConsumerSecret), TwitterAPI.UrlEncode(UserSecret));
				else
					hash_key = string.Format("{0}&", TwitterAPI.UrlEncode(ConsumerSecret));

				hash_parameter = String.Format(
						"{0}&{1}&{2}",
						http_method,
						TwitterAPI.UrlEncode(URL),
						TwitterAPI.UrlEncode(hash_parameter)
						);

				using (HMACSHA1 oCrypt = new HMACSHA1())
				{
					oCrypt.Key = Encoding.UTF8.GetBytes(hash_key);
					oauth_signature = Convert.ToBase64String(oCrypt.ComputeHash(Encoding.UTF8.GetBytes(hash_parameter)));
				}

				// OAuth 생성
				dic.Add("oauth_signature", oauth_signature);

				StringBuilder sbOAuth = new StringBuilder();
				sbOAuth.Append("OAuth ");
				foreach (KeyValuePair<string, string> st in this.dic)
					if (Array.IndexOf<string>(oauth_array, st.Key) >= 0)
						sbOAuth.AppendFormat("{0}=\"{1}\",", st.Key, TwitterAPI.UrlEncode(st.Value));
				sbOAuth.Remove(sbOAuth.Length - 1, 1);

				// 웹 호출
				using (WebClient oWeb = new WebClient())
				{
					oWeb.Headers.Add("Authorization", sbOAuth.ToString());

					if (isPost)
					{
						oWeb.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
						responseBody = oWeb.UploadString(URL, "POST", sbPost.ToString());
					}
					else
					{
						responseBody = oWeb.DownloadString(URL + "?" + sbPost.ToString());
					}
				}
			}
			catch (WebException ex)
			{
				if (GetErrorMsg)
					using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
						responseBody = sr.ReadToEnd();
				else
					responseBody = null;
			}
			catch
			{
				responseBody = null;
			}

			this.dic.Clear();
			this.CallBackURL = null;

			return responseBody;
		}
		
		//////////////////////////////////////////////////////////////////////////////////////////
		public static string ParseQueryStringParameter(string parameterName, string text)
		{
			//Match expressionMatch = Regex.Match(text, string.Format(@"{0}=(?<value>[^&]+)", parameterName));
			Match expressionMatch = Regex.Match(text, string.Format(@"{0}=([^&]+)", parameterName));

			if (!expressionMatch.Success)
				return string.Empty;

			return expressionMatch.Groups[1].Value;
		}
		static Regex UrlEncodeReg = new Regex("(%[0-9a-f][0-9a-f])", RegexOptions.Compiled);
		public static string UrlEncode(string value)
		{
			try
			{
				value = Uri.EscapeDataString(value).Replace("(", "%28").Replace(")", "%29").Replace("$", "%24").Replace("!", "%21").Replace("*", "%2A").Replace("'", "%27").Replace("%7E", "~");
				value = UrlEncodeReg.Replace(value, c => c.Value.ToUpper()).Replace("%EF%BB%BF", "");
				return value;
			}
			catch
			{
				return value;
			}
		}
		static Random rnd = new Random(DateTime.Now.Millisecond);
		public static string GetNonce()
		{
			return rnd.Next(0, 0x7FFFFFFF).ToString("X");
		}
		static DateTime GenerateTimeStampDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		public static string GenerateTimeStamp()
		{
			return Convert.ToInt64((DateTime.UtcNow - GenerateTimeStampDateTime).TotalSeconds).ToString();
		}
	}
}
