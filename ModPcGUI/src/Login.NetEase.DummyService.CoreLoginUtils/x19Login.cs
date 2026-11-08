using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Login.Native;
using Login.PEEntity.PEAuthentication;
using Login.loginauth;
using MCStudio.Network.Http;
using Mark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Extensions;
using Noya.LocalServer.Common.Utilities;
using WPFLauncher.Network.Launcher;
using text;
using text.loginauth;

namespace Login.NetEase.DummyService.CoreLoginUtils;

internal class x19Login
{
	public int Offset = 2;

	public int Rounds = 9;

	private Thread UpdateAuth;

	public Action<byte[]> UpdateCallBack;

	private readonly HttpClient client = new HttpClient();

	public string UID;

	public string PEUID;

	public byte[] MD5Token;

	public static bool delete { get; set; }

	public static AuthenticationResponse AuthenticationEntity { get; set; }

	public string sa_data { get; set; } = "{\"os_name\":\"windows\",\"os_ver\":\"Microsoft Windows 10 专业版\",\"mac_addr\":\"B8975A4AD616\",\"udid\":\"BFEBFBFF000306A9C78C00D8\",\"app_ver\":\"1.14.6.45947\",\"sdk_ver\":\"\",\"network\":\"\",\"disk\":\"C78C00D8\",\"is64bit\":\"1\",\"video_card1\":\"Video_cardl\",\"video_card2\":\"\",\"video_card3\":\"\",\"video_card4\":\"\",\"launcher_type\":\"PC_java\",\"pay_channel\":\"4399pc\",\"dotnet_ver\":\"4.8.0\",\"cpu_type\":\"Intel(R) Xeon(R) CPU i32100 3.10GHz\",\"ram_size\":\"8553332736\",\"device_width\":\"1920\",\"device_height\":\"1080\",\"os_detail\":\"10\"}";

	public string sa_data_PE { get; set; } = "{\"app_channel\":\"netease\",\"app_ver\":\"3.1.12.261439\",\"core_num\":\"8\",\"cpu_digit\":\"64\",\"cpu_hz\":\"1882000\",\"cpu_name\":\"vendor Kirin810\",\"device_height\":\"2000\",\"device_model\":\"HUAWEI BAH3-W09\",\"device_width\":\"1200\",\"disk\":\"\",\"emulator\":0,\"first_udid\":\"11ff2c22e0b4b5a6\",\"is_guest\":0,\"launcher_type\":\"PE_C++\",\"mac_addr\":\"02:00:00:00:00:00\",\"network\":\"CHANNEL_UNKNOW\",\"os_name\":\"android\",\"os_ver\":\"7.1.2\",\"ram\":\"6130167808\",\"rom\":\"114965872640\",\"root\":false,\"sdk_ver\":\"5.2.0\",\"start_type\":\"default\",\"udid\":\"11ff2c22e0b4b5a6\"}";

	public string server_id { get; set; }

	public string room_id_public { get; set; }

	public bool istrue { get; set; }

	public string LoginSRCToken { get; set; }

	public string LoginDToken { get; set; }

	public string player_info { get; set; }

	public string AuthResponse { get; set; }

	public string coreobt { get; set; } = "https://x19obtcore.nie.netease.com:8443";

	public string apigatewayobt { get; set; } = "https://x19apigatewayobt.nie.netease.com";

	public string apigatewayobtPE { get; set; } = "https://g79apigatewayobt.minecraft.cn";

	public static string engineVersion { get; set; } = app.Temp.engineVersion;

	public static string patchVersion { get; set; } = app.Temp.patchVersion;

	public static string PECoreobt { get; set; } = "https://g79obtcore.minecraft.cn:8443";

	public string LibMinecraftPE { get; set; } = app.Temp.libminecraftpe;

	public string Patch { get; set; } = app.Temp.patch;

	public x19Login()
	{
		client.DefaultRequestHeaders.ExpectContinue = false;
		client.DefaultRequestHeaders.Add("User-Agent", "WPFLauncher/0.0.0.0");
	}

	public int Login(string Login_otp)
	{
		_ = string.Empty;
		LoginotpResposne loginotpResposne = new LoginotpResposne();
		new AuthenticationResponse();
		if (Login_otp == "")
		{
			Term.WriteLine("未检测到cookie");
			throw new ModPcFatalException("程序终止：登录流程中断");
		}
		StringContent content = new StringContent(Login_otp, Encoding.UTF8, "application/json");
		string result = client.PostAsync(coreobt + "/login-otp", content).Result.Content.ReadAsStringAsync().Result;
		try
		{
			loginotpResposne = JsonConvert.DeserializeObject<LoginotpResposne>(result);
		}
		catch
		{
			Term.WriteLine("请求失败" + result);
			return -1;
		}
		if (loginotpResposne.code != 0)
		{
			Term.WriteLine("登录失败，请检查cookie：" + Regex.Unescape(result));
			return -1;
		}
		string authenticationOtpJson = GetAuthenticationOtpJson("1.14.6.45947", loginotpResposne.entity.aid, loginotpResposne.entity.otp_token, Login_otp);
		ByteArrayContent byteArrayContent = new ByteArrayContent(x19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(authenticationOtpJson)));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		string text = (AuthResponse = CoreNative.HttpDecrypt(client.PostAsync(coreobt + "/authentication-otp", byteArrayContent).Result.Content.ReadAsByteArrayAsync().Result, out var _));
		string text3 = text;
		try
		{
			AuthenticationEntity = JsonConvert.DeserializeObject<AuthenticationResponse>(text3);
			UID = AuthenticationEntity.entity.entity_id;
			LoginSRCToken = AuthenticationEntity.entity.token;
			MD5Token = HashHelper.SafeCompleteMD5(Encoding.UTF8.GetBytes(LoginSRCToken));
		}
		catch
		{
			Term.WriteLine("出错了？请切换账号后重试！");
			Term.WriteLine("错误信息：" + Regex.Unescape(text3));
			Thread.Sleep(5000);
			return -1;
		}
		Term.Clear();
		Term.WriteLine("登录成功！");
		UpdateLogin_();
		GetName();
		Term.WriteLine("UID:" + UID);
		return 0;
	}

	public static string Base64Encode(byte[] plainText)
	{
		return Convert.ToBase64String(plainText);
	}

	public int PELogin(string Login_otp)
	{
		string guid = GetGuid();
		string message = engineVersion + LibMinecraftPE + patchVersion + Patch + guid;
		PEAURequest pEAURequest = new PEAURequest();
		pEAURequest.sa_data = sa_data_PE;
		pEAURequest.engine_version = engineVersion;
		pEAURequest.patch_version = patchVersion;
		pEAURequest.message = message;
		pEAURequest.sauth_json = JsonConvert.DeserializeObject<JObject>(Login_otp)["sauth_json"];
		pEAURequest.seed = guid;
		pEAURequest.sign = Base64Encode(CoreSign.PESignCount(message));
		StringContent content = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pEAURequest))).ToHex(), Encoding.UTF8, "application/json");
		string result = client.PostAsync(PECoreobt + "/pe-authentication", content).Result.Content.ReadAsStringAsync().Result;
		AuthResponse = x19Crypt.HttpDecrypt_g79v12_(result.HexToBytes());
		try
		{
			AuthenticationEntity = JsonConvert.DeserializeObject<AuthenticationResponse>(AuthResponse);
			UID = AuthenticationEntity.entity.entity_id;
			LoginSRCToken = AuthenticationEntity.entity.token;
			MD5Token = HashHelper.SafeCompleteMD5(Encoding.UTF8.GetBytes(LoginSRCToken));
		}
		catch (Exception ex)
		{
			Term.WriteLine("出错了？请切换账号后重试！");
			Term.WriteLine("登录响应：" + Regex.Unescape(AuthResponse));
			Term.WriteLine("错误信息：" + ex.ToString());
			return -1;
		}
		Term.WriteLine("登录成功！");
		UpdateLogin_();
		GetName();
		Term.WriteLine("UID:" + UID);
		return 0;
	}

	public static string GetGuid()
	{
		return Guid.NewGuid().ToString();
	}

	public void GetName()
	{
		string value = x19Crypt.ComputeDynamicToken("/user-detail", "", LoginSRCToken);
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		string text = Regex.Unescape(client.PostAsync(apigatewayobt + "/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result);
		player_info = text;
	}

	public void UpdateLogin_()
	{
		delete = true;
		try
		{
			UpdateAuth = new Thread(() =>
			{
				while (delete)
				{
					Thread.Sleep(300000);
					UpdateAuthotp(coreobt);
				}
			});
			UpdateAuth.Start();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
		}
	}

	public string postAPIPC(string requests, string url)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, LoginSRCToken);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(apigatewayobt + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Term.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public void Close()
	{
		try
		{
			UpdateAuth.Abort();
		}
		catch (Exception)
		{
		}
		try
		{
			Delete();
		}
		catch (Exception)
		{
		}
	}

	public void Delete()
	{
		string text = JsonConvert.SerializeObject(new AuthenticationDelete
		{
			user_id = UID
		});
		string value = x19Crypt.ComputeDynamicToken("/authentication/delete", text, LoginSRCToken);
		HttpContent httpContent = new StringContent(text);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		_ = client.PostAsync(app.Temp.url + "/authentication/delete", httpContent).Result.Content.ReadAsStringAsync().Result;
	}

	public string postAPIPE(string requests, string url)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, LoginSRCToken);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(apigatewayobtPE + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Term.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public string x19POST(string host, string requests, string url)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, LoginSRCToken);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Term.WriteLine(Regex.Unescape(result));
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public string GetAuthenticationOtpJson(string versionvaluas, string aid, string otp_token, string Sauth_json)
	{
		try
		{
			AuthoptSauth authoptSauth = new AuthoptSauth();
			VersionEntity versionEntity = new VersionEntity();
			AuthenticationEntity authenticationEntity = new AuthenticationEntity
			{
				sa_data = sa_data
			};
			authoptSauth = JsonConvert.DeserializeObject<AuthoptSauth>(Sauth_json);
			authenticationEntity.sauth_json = authoptSauth.sauth_json;
			versionEntity.version = versionvaluas;
			authenticationEntity.version = versionEntity;
			authenticationEntity.aid = aid;
			authenticationEntity.otp_token = otp_token;
			return JsonConvert.SerializeObject(authenticationEntity);
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public string UpdateAuthotp(string url)
	{
		try
		{
			_ = string.Empty;
			string authenticationUpdate = GetAuthenticationUpdate(UID);
			string value = x19Crypt.ComputeDynamicToken("/authentication/update", authenticationUpdate, LoginSRCToken);
			HttpContent httpContent = new ByteArrayContent(x19Crypt.HttpEncrypt(Encoding.ASCII.GetBytes(authenticationUpdate)));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = CoreNative.HttpDecrypt(client.PostAsync(url + "/authentication/update", httpContent).Result.Content.ReadAsByteArrayAsync().Result, out var _);
			LoginSRCToken = JsonConvert.DeserializeObject<JObject>(text)["entity"]["token"].ToString();
			MD5Token = HashHelper.SafeCompleteMD5(Encoding.UTF8.GetBytes(LoginSRCToken));
			return text;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public string GetAuthenticationUpdate(string uid)
	{
		return JsonConvert.SerializeObject(new AuthenticationEntity
		{
			entity_id = uid
		});
	}
}
