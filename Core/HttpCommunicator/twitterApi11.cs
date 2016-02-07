using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;

namespace Twitter
{
	public static class TwitterAPI11
	{
		public static string consumerToken;
		public static string consumerSecret;

		public static string ParseJsonString(string parameterName, string text)
		{
			Match expressionMatch = Regex.Match(text, string.Format("\"{0}\"[ ]*:[ ]*\"(?<value>[^\"]+)\"", parameterName));

			if (!expressionMatch.Success)
				return null;
			else
				return expressionMatch.Groups["value"].Value.Replace(@"\", "");
		}

		public static class OAuth
		{
			public static bool request_token(
				string callback,
				out string outToken,
				out string outSecret)
			{
				try
				{
					using (TwitterAPI api = new TwitterAPI(TwitterAPI11.consumerToken, TwitterAPI11.consumerSecret))
					{
						api.CallBackURL = callback;
						string responseBody = api.GetResponsePOST("https://api.twitter.com/oauth/request_token");

						outToken = TwitterAPI.ParseQueryStringParameter("oauth_token", responseBody);
						outSecret = TwitterAPI.ParseQueryStringParameter("oauth_token_secret", responseBody);
					}

					return true;
				}
				catch
				{
					outToken = null; outSecret = null;

					return false;
				}
			}
			
			public static bool access_token(
				string token,
				string secret,
				string oauth_verifier,
				out string outToken,
				out string outSecret)
			{
				string outId, outName;
				return access_token(token, secret, oauth_verifier, out outToken, out outSecret, out outId, out outName);
			}

			public static bool access_token(
				string token,
				string secret,
				string oauth_verifier,
				out string outToken,
				out string outSecret,
				out string outId,
				out string outName)
			{
				try
				{
					using (TwitterAPI api = new TwitterAPI(TwitterAPI11.consumerToken, TwitterAPI11.consumerSecret, token, secret))
					{
						api["oauth_verifier"] = oauth_verifier;
						string responseBody = api.GetResponsePOST("https://api.twitter.com/oauth/access_token");

						outToken = TwitterAPI.ParseQueryStringParameter("oauth_token", responseBody);
						outSecret = TwitterAPI.ParseQueryStringParameter("oauth_token_secret", responseBody);
						outId = TwitterAPI.ParseQueryStringParameter("user_id", responseBody);
						outName = TwitterAPI.ParseQueryStringParameter("screen_name", responseBody);
					}

					return true;
				}
				catch
				{
					outToken = null;
					outSecret = null;
					outId = null;
					outName = null;
					return false;
				}
			}
		}
		public static class Statuses
		{
			public static bool update(string token, string secret, string format, params object[] args)
			{
				return update(token, secret, String.Format(format, args));
			}
			public static bool update(string token, string secret, string status)
			{
				try
				{
					using (TwitterAPI api = new TwitterAPI(TwitterAPI11.consumerToken, TwitterAPI11.consumerSecret, token, secret))
					{
						api["status"] = status;

						return (api.GetResponsePOST("http://api.twitter.com/1.1/statuses/update.json") != null);
					}
				}
				catch
				{
					return false;
				}
			}
		}

		public static class Account
		{
			public static bool verify_credentials(
				string token,
				string secret,
				out string outName,
				out string outProfileImage)
			{
				string outID;
				return verify_credentials(token, secret, out outID, out outName, out outProfileImage);
			}
			public static bool verify_credentials(
				string token,
				string secret,
				out string outID,
				out string outName,
				out string outProfileImage)
			{
				try
				{
					using (TwitterAPI api = new TwitterAPI(TwitterAPI11.consumerToken, TwitterAPI11.consumerSecret, token, secret))
					{
						string responseBody = api.GetResponseGET("https://api.twitter.com/1.1/account/verify_credentials.json");

						outID = ParseJsonString("id_str", responseBody);
						outName = ParseJsonString("screen_name", responseBody);
						outProfileImage = ParseJsonString("profile_image_url_https", responseBody).Replace("_normal.", ".");
					}

					return true;
				}
				catch
				{
					outID = null;
					outName = null;
					outProfileImage = null;

					return false;
				}
			}
		}

		public static class Users
		{
			public static bool show(
				string token,
				string secret,
				string userId,
				string screenName,
				out string outName)
			{
				string sNull;
				return show(token, secret, userId, screenName, out outName, out sNull);
			}

			public static bool show(
				string token,
				string secret,
				string userId,
				string screenName,
				out string outName,
				out string outProfileImage)
			{
				try
				{
					using (TwitterAPI api = new TwitterAPI(TwitterAPI11.consumerToken, TwitterAPI11.consumerSecret, token, secret))
					{
						if (userId != null)
							api["user_id"] = userId;
						else
							api["screen_name"] = screenName;

						string responseBody = api.GetResponseGET("https://api.twitter.com/1.1/users/show.json");

						outName = ParseJsonString("screen_name", responseBody);
						outProfileImage = ParseJsonString("profile_image_url_https", responseBody).Replace("_normal.", ".");

					}

					return true;
				}
				catch
				{
					outName = null;
					outProfileImage = null;

					return false;
				}
			}
		}
	}
}
