using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Login.NetEase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JS4399MC;

public class JS4399
{
	private const string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0";

	private static readonly Random _random = new Random();

	private static JS4399HttpConfig _httpConfig;

	public JS4399(JS4399HttpConfig httpConfig = null)
	{
		_httpConfig = httpConfig ?? new JS4399HttpConfig();
	}

	public async Task<JS4399Result> JS4399RegisterAsync(Dictionary<string, object> registerConfig = null, Func<string, Task<string>> captchaFunctionAsync = null)
	{
		JS4399Result result = new JS4399Result();
		try
		{
			string username = ((registerConfig?.ContainsKey("username") ?? false) ? registerConfig["username"].ToString() : GenerateRandomString());
			string password = ((registerConfig?.ContainsKey("password") ?? false) ? registerConfig["password"].ToString() : GenerateRandomString());
			string captchaID = "captchaReqb3d25c6d6a4" + GenerateRandomString(8, new Dictionary<string, object> { { "numbers", true } });
			HttpClient httpClient = new HttpClient(new HttpClientHandler
			{
				Proxy = _httpConfig.Proxy,
				UseProxy = (_httpConfig.Proxy != null)
			})
			{
				DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0" } }
			};
			HttpRequestMessage request = new HttpRequestMessage(requestUri: "https://ptlogin.4399.com/ptlogin/captcha.do?xx=1&captchaId=" + captchaID, method: HttpMethod.Get);
			HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(request);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				result.Message = "访问验证码接口失败";
				return result;
			}
			string arg = Convert.ToBase64String(await httpResponseMessage.Content.ReadAsByteArrayAsync());
			string text = await captchaFunctionAsync(arg);
			string text2 = "110108" + GetRandomDate("19700101", "20041231") + GenerateRandomString(3, new Dictionary<string, object> { { "numbers", true } });
			text2 += GetIDCardLastCode(text2);
			string str = GenerateRandomString(1, new Dictionary<string, object> { { "custom", "李王张刘陈杨赵黄周吴徐孙胡朱高林何郭马罗梁宋郑谢韩唐冯于董萧程曹袁邓许傅沈曾彭吕苏卢蒋蔡贾丁魏薛叶阎余潘杜戴夏钟汪田任姜范方石姚谭廖邹熊金陆郝孔白崔康毛邱秦江史顾侯邵孟龙万段漕钱汤尹黎易常武乔贺赖龚文" } }) + GenerateRandomString(2, new Dictionary<string, object> { { "chinese", true } });
			request = new HttpRequestMessage(requestUri: "https://ptlogin.4399.com/ptlogin/register.do?postLoginHandler=default&displayMode=popup&appId=www_home&gameId=&cid=&externalLogin=qq&aid=&ref=&css=&redirectUrl=&regMode=reg_normal&sessionId=" + captchaID + "&regIdcard=true&noEmail=false&crossDomainIFrame=&crossDomainUrl=&mainDivId=popup_reg_div&showRegInfo=true&includeFcmInfo=false&expandFcmInput=true&fcmFakeValidate=true&userNameLabel=4399%E7%94%A8%E6%88%B7%E5%90%8D&username=" + username + "&password=" + password + "&realname=" + HttpUtility.UrlEncode(str) + "&idcard=" + text2 + "&email=" + GenerateRandomString(9, new Dictionary<string, object> { { "numbers", true } }) + "@qq.com&reg_eula_agree=on&inputCaptcha=" + text, method: HttpMethod.Get);
			HttpResponseMessage httpResponseMessage2 = await httpClient.SendAsync(request);
			if (!httpResponseMessage2.IsSuccessStatusCode)
			{
				result.Message = "访问注册接口失败";
				return result;
			}
			string text3 = await httpResponseMessage2.Content.ReadAsStringAsync();
			if (text3.Contains("验证码错误"))
			{
				result.Message = "验证码错误";
				return result;
			}
			if (text3.Contains("用户名格式错误"))
			{
				result.Message = "用户名格式错误";
				return result;
			}
			if (text3.Contains("用户名已被注册"))
			{
				result.Message = "用户名已被注册";
				return result;
			}
			if (!text3.Contains("请一定记住您注册的用户名和密码"))
			{
				result.Message = "未知错误";
				return result;
			}
			result.Success = true;
			result.Message = "注册成功";
			result.Username = username;
			result.Password = password;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			result.Message = ex2.Message;
		}
		return result;
	}

	public async Task<JS4399Result> JS4399LoginAsync(Dictionary<string, object> loginConfig, Func<string, Task<string>> captchaFunctionAsync)
	{
		JS4399Result result = new JS4399Result();
		try
		{
			string username = loginConfig["username"].ToString();
			string password = loginConfig["password"].ToString();
			string text = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
			HttpClient httpClient = new HttpClient(new HttpClientHandler
			{
				Proxy = _httpConfig.Proxy,
				UseProxy = (_httpConfig.Proxy != null)
			})
			{
				DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0" } }
			};
			Function.ClientLog("[4399SDK]ptlogin verify");
			string requestUri = "https://ptlogin.4399.com/ptlogin/verify.do?username=" + username + "&appId=kid_wdsj&t=" + text + "&inputWidth=iptw2&v=1";
			HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(requestUri);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				result.Message = "访问验证接口失败";
				Function.ClientError(result.Message);
				return result;
			}
			string text2 = await httpResponseMessage.Content.ReadAsStringAsync();
			string text3 = null;
			string captchaID = null;
			if (text2 != "0")
			{
				captchaID = GetBetweenStrings(text2, "captchaId=", "'");
				if (string.IsNullOrEmpty(captchaID))
				{
					result.Message = "获取captchaID失败";
					Function.ClientError(result.Message);
					return result;
				}
				Function.ClientLog("[4399SDK]ptlogin captcha");
				HttpResponseMessage httpResponseMessage2 = await httpClient.GetAsync("https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaID);
				if (!httpResponseMessage2.IsSuccessStatusCode)
				{
					result.Message = "获取验证码失败";
					Function.ClientError(result.Message);
					return result;
				}
				string arg = Convert.ToBase64String(await httpResponseMessage2.Content.ReadAsByteArrayAsync());
				text3 = await captchaFunctionAsync(arg);
			}
			string text4 = "loginFrom=uframe&postLoginHandler=default&layoutSelfAdapting=true&externalLogin=qq&displayMode=popup&layout=vertical&bizId=2100001792&appId=kid_wdsj&gameId=wd&css=http%3A%2F%2Fmicrogame.5054399.net%2Fv2%2Fresource%2FcssSdk%2Fdefault%2Flogin.css&redirectUrl=&sessionId=" + captchaID + "&mainDivId=popup_login_div&includeFcmInfo=false&level=8&regLevel=8&userNameLabel=4399%E7%94%A8%E6%88%B7%E5%90%8D&userNameTip=%E8%AF%B7%E8%BE%93%E5%85%A54399%E7%94%A8%E6%88%B7%E5%90%8D&welcomeTip=%E6%AC%A2%E8%BF%8E%E5%9B%9E%E5%88%B04399&sec=1&password=" + password + "&username=" + username + "&inputCaptcha=" + text3;
			StringContent content = new StringContent(text4, Encoding.UTF8, "application/x-www-form-urlencoded");
			Function.ClientLog("[4399SDK]ptlogin login");
			Function.ClientLog("[4399SDK]ptlogin LoginData:" + text4);
			HttpResponseMessage loginResponse = await httpClient.PostAsync("https://ptlogin.4399.com/ptlogin/login.do?v=1", content);
			if (!loginResponse.IsSuccessStatusCode)
			{
				result.Message = "登录请求失败";
				Function.ClientError(result.Message);
				return result;
			}
			string text5 = await loginResponse.Content.ReadAsStringAsync();
			if (text5.Contains("验证码错误"))
			{
				result.Message = "验证码错误";
				Function.ClientError(result.Message);
				return result;
			}
			if (text5.Contains("密码错误"))
			{
				result.Message = "密码错误";
				Function.ClientError(result.Message);
				return result;
			}
			if (text5.Contains("用户不存在"))
			{
				result.Message = "用户不存在";
				Function.ClientError(result.Message);
				return result;
			}
			string randtime = GetBetweenStrings(text5, "parent.timestamp = \"", "\"");
			if (string.IsNullOrEmpty(randtime))
			{
				result.Message = "获取时间戳失败";
				Function.ClientError(result.Message);
				return result;
			}
			Function.ClientLog("[4399SDK]Set Cookie");
			List<string> list = loginResponse.Headers.GetValues("Set-Cookie").ToList();
			if (list == null || list.Count == 0)
			{
				result.Message = "Cookie获取失败";
				Function.ClientError(result.Message);
				return result;
			}
			string text6 = string.Join("; ", list.Select((string cookie) => cookie.Split(';')[0]));
			CookieContainer cookieContainer = new CookieContainer();
			Function.ClientLog("[4399SDK]checkKidLoginUserCookie");
			Uri uri = new Uri("https://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do");
			string[] array = text6.Split(';');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split('=');
				if (array2.Length == 2)
				{
					cookieContainer.Add(uri, new Cookie(array2[0].Trim(), array2[1].Trim()));
				}
			}
			Function.ClientLog("[4399SDK]Second login check");
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(requestUri: "https://ptlogin.4399.com/ptlogin/checkKidLoginUserCookie.do?appId=kid_wdsj&gameUrl=http://cdn.h5wan.4399sj.com/microterminal-h5-frame?game_id=500352&rand_time=" + randtime + "&nick=null&onLineStart=false&show=1&isCrossDomain=1&retUrl=http%253A%252F%252Fptlogin.4399.com%252Fresource%252Fucenter.html%253Faction%253Dlogin%2526appId%253Dkid_wdsj%2526loginLevel%253D8%2526regLevel%253D8%2526bizId%253D2100001792%2526externalLogin%253Dqq%2526qrLogin%253Dtrue%2526layout%253Dvertical%2526level%253D101%2526css%253Dhttp%253A%252F%252Fmicrogame.5054399.net%252Fv2%252Fresource%252FcssSdk%252Fdefault%252Flogin.css%2526v%253D2018_11_26_16%2526postLoginHandler%253Dredirect%2526checkLoginUserCookie%253Dtrue%2526redirectUrl%253Dhttp%25253A%25252F%25252Fcdn.h5wan.4399sj.com%25252Fmicroterminal-h5-frame%25253Fgame_id%25253D500352%252526rand_time%25253D" + randtime, method: HttpMethod.Get)
			{
				Headers = { { "Cookie", text6 } }
			};
			HttpResponseMessage httpResponseMessage3 = null;
			using (HttpClient tmpClient = new HttpClient(new HttpClientHandler
			{
				Proxy = _httpConfig.Proxy,
				UseProxy = (_httpConfig.Proxy != null),
				AllowAutoRedirect = false,
				CookieContainer = cookieContainer
			}))
			{
				tmpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/134.0");
				httpResponseMessage3 = await tmpClient.SendAsync(httpRequestMessage);
			}
			if (httpResponseMessage3.StatusCode != HttpStatusCode.Found)
			{
				result.Message = "检查登录状态失败";
				Function.ClientError(result.Message);
				return result;
			}
			string text7 = httpResponseMessage3.Headers.Location?.ToString();
			if (string.IsNullOrEmpty(text7))
			{
				result.Message = "获取重定向地址失败";
				Function.ClientError(result.Message);
				return result;
			}
			Uri uri2 = new Uri(text7);
			if (uri2.Host != "cdn.h5wan.4399sj.com")
			{
				result.Message = "重定向域名错误";
				Function.ClientError(result.Message);
				return result;
			}
			NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(uri2.Query);
			string text8 = nameValueCollection["sig"];
			string uid = nameValueCollection["uid"];
			string time = nameValueCollection["time"];
			string text9 = nameValueCollection["validateState"];
			if (string.IsNullOrEmpty(text8) || string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(time) || string.IsNullOrEmpty(text9))
			{
				result.Message = "解析重定向参数失败";
				Function.ClientError(result.Message);
				return result;
			}
			Function.ClientLog("[4399SDK]Get SDK info");
			string requestUri3 = "https://microgame.5054399.net/v2/service/sdk/info?callback=&queryStr=game_id%3D500352%26nick%3Dnull%26sig%3D" + text8 + "%26uid%3D" + uid + "%26fcm%3D0%26show%3D1%26isCrossDomain%3D1%26rand_time%3D" + randtime + "%26ptusertype%3D4399%26time%3D" + time + "%26validateState%3D" + text9 + "%26username%3D" + username.ToLower() + "&_=" + time;
			HttpResponseMessage httpResponseMessage4 = await httpClient.GetAsync(requestUri3);
			if (!httpResponseMessage4.IsSuccessStatusCode)
			{
				result.Message = "获取SDK信息失败";
				Function.ClientError(result.Message);
				return result;
			}
			string text10 = JObject.Parse(await httpResponseMessage4.Content.ReadAsStringAsync())["data"]?["sdk_login_data"]?.ToString();
			if (string.IsNullOrEmpty(text10))
			{
				result.Message = "解析SDK数据失败";
				Function.ClientError(result.Message);
				return result;
			}
			string text11 = HttpUtility.ParseQueryString(text10)["token"];
			if (string.IsNullOrEmpty(text11))
			{
				result.Message = "获取token失败";
				Function.ClientError(result.Message);
				return result;
			}
			string[] array3 = (from _ in Enumerable.Range(0, 2)
				select GenerateRandomString(32, new Dictionary<string, object> { { "custom", "0123456789ABCDEF" } })).ToArray();
			var value = new
			{
				aim_info = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}",
				app_channel = "4399pc",
				client_login_sn = array3[0],
				deviceid = array3[1],
				gameid = "x19",
				gas_token = "",
				ip = "127.0.0.1",
				login_channel = "4399pc",
				platform = "pc",
				realname = "{\"realname_type\":\"0\"}",
				sdk_version = "1.0.0",
				sdkuid = uid,
				sessionid = text11,
				source_platform = "pc",
				timestamp = time,
				udid = array3[1],
				userid = username.ToLower()
			};
			string sauthJsonValue = JsonConvert.SerializeObject(value);
			string sauthJson = JsonConvert.SerializeObject(new
			{
				sauth_json = sauthJsonValue
			});
			Function.ClientLog("[4399SDK]Final login requests");
			httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth")
			{
				Headers = { { "User-Agent", "WPFLauncher/0.0.0.0" } }
			};
			content = new StringContent(sauthJsonValue, Encoding.UTF8, "application/json");
			httpRequestMessage.Content = content;
			if (!(await httpClient.SendAsync(httpRequestMessage)).IsSuccessStatusCode)
			{
				result.Message = "统一认证请求失败";
				Function.ClientError(result.Message);
				return result;
			}
			Function.ClientLog("[4399SDK][SAUTH_JSON]:" + sauthJson);
			result.Success = true;
			result.Message = "登录成功";
			result.SauthJson = sauthJson;
			result.SauthJsonValue = sauthJsonValue;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			result.Message = ex2.Message;
		}
		return result;
	}

	private string GetBetweenStrings(string str, string start, string end)
	{
		int num = str.IndexOf(start, StringComparison.Ordinal);
		if (num == -1)
		{
			return null;
		}
		num += start.Length;
		int num2 = str.IndexOf(end, num, StringComparison.Ordinal);
		if (num2 != -1)
		{
			return str.Substring(num, num2 - num);
		}
		return null;
	}

	public string GenerateRandomString(int length = 10, Dictionary<string, object> options = null)
	{
		options = options ?? new Dictionary<string, object>
		{
			{ "numbers", true },
			{ "lowercase", true }
		};
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		bool flag2 = false;
		if (options.ContainsKey("custom") && options["custom"].ToString() != "")
		{
			stringBuilder.Append(options["custom"].ToString());
			flag = false;
		}
		else
		{
			if (options.ContainsKey("numbers") && (bool)options["numbers"])
			{
				stringBuilder.Append("0123456789");
				flag = false;
			}
			if (options.ContainsKey("lowercase") && (bool)options["lowercase"])
			{
				stringBuilder.Append("abcdefghijklmnopqrstuvwxyz");
				flag = false;
			}
			if (options.ContainsKey("uppercase") && (bool)options["uppercase"])
			{
				stringBuilder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
				flag = false;
			}
			if (options.ContainsKey("symbols") && (bool)options["symbols"])
			{
				stringBuilder.Append("!@#$%^&*()-_=+[]{}|;:,.<>?/");
				flag = false;
			}
			flag2 = options.ContainsKey("chinese") && (bool)options["chinese"];
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		if (stringBuilder.Length == 0 && !flag2)
		{
			throw new ArgumentException("Character pool is empty. Please ensure options contain at least one character set.");
		}
		for (int i = 0; i < length; i++)
		{
			if ((!options.ContainsKey("custom") || !(options["custom"].ToString() != "")) && (flag || (flag2 && _random.NextDouble() > 0.5)))
			{
				stringBuilder2.Append(GenerateChineseCharacter());
				continue;
			}
			int index = _random.Next(stringBuilder.Length);
			stringBuilder2.Append(stringBuilder.ToString()[index]);
		}
		return stringBuilder2.ToString();
	}

	private static char GenerateChineseCharacter()
	{
		return (char)_random.Next(19968, 40870);
	}

	private string GetRandomDate(string startDate, string endDate)
	{
		DateTime dateTime = DateTime.ParseExact(startDate, "yyyyMMdd", CultureInfo.InvariantCulture);
		int days = (DateTime.ParseExact(endDate, "yyyyMMdd", CultureInfo.InvariantCulture) - dateTime).Days;
		return dateTime.AddDays(_random.Next(days)).ToString("yyyyMMdd");
	}

	private string GetIDCardLastCode(string idCard)
	{
		int[] factors = new int[17]
		{
			7, 9, 10, 5, 8, 4, 2, 1, 6, 3,
			7, 9, 10, 5, 8, 4, 2
		};
		string[] array = new string[11]
		{
			"1", "0", "X", "9", "8", "7", "6", "5", "4", "3",
			"2"
		};
		int num = idCard.Take(17).Select((char c, int i) => (c - 48) * factors[i]).Sum();
		return array[num % 11];
	}
}
