using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace FeverToSauth;

internal class HttpHelper
{
	private static HttpClient HttpClient { get; set; }

	public static void InitHttp()
	{
		HttpClient = new HttpClient();
		HttpClient.DefaultRequestHeaders.ExpectContinue = false;
	}

	public static string postAPImclobtPE(string url, string requests)
	{
		try
		{
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
			string result = HttpClient.PostAsync(url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Term.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}
}
