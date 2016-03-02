using System;
using System.Text.RegularExpressions;

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
	}
}
