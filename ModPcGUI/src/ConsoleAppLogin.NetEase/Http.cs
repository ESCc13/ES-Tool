using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FeverToSauth;
using Login.JavaHelper;
using Login.Native;
using Login.NetEase;
using Login.NetEase.Entity;
using Login.NetEase.LinkService;
using Login.NeteaseEntity;
using Login.PEEntity;
using Login.PEEntity.PEAuthentication;
using Login.loginauth;
using Login.loginauth.authentication_v2;
using MCStudio.Network.Http;
using Mark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Extensions;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using text;
using text.loginauth;

namespace ConsoleAppLogin.NetEase;

internal class Http
{
	public static readonly HttpClient client = new HttpClient();

	public static HttpListener httpobj;

	public static bool EncryptionAuthentication = true;

	public static bool IsAuthServer = false;

	public static string AuthServerUrl { get; set; }

	public static string server_id { get; set; }

	public static string server_ip { get; set; }

	public static string room_id_public { get; set; }

	public static bool istrue { get; set; }

	private static string response_data { get; set; }

	public static string UID { get; set; }

	public static string PEUID { get; set; }

	public static string URL { get; set; } = "https://x19apigatewayobt.nie.netease.com";

	public static string LoginSRCToken { get; set; }

	public static string LoginDToken { get; set; }

	public static Thread Thread { get; private set; }

	public static void Login(string Login_otp)
	{
		if (Thread != null)
		{
			Close();
		}
		if (Login_otp.StartsWith("SDK4399"))
		{
			Login_otp = FeverAuth.SDK4399ToSauth(Login_otp);
		}
		if (Login_otp != null)
		{
			Function.ClientLog("[Login] create Login type:x19");
			string keyStr = string.Empty;
			LoginotpResposne loginotpResposne = new LoginotpResposne();
			new AuthenticationResponse();
			if (Login_otp == "")
			{
				Term.WriteLine("未检测到cookie");
			}
			StringContent content = new StringContent(Login_otp, Encoding.UTF8, "application/json");
			string result = client.PostAsync(app.Temp.url + "/login-otp", content).Result.Content.ReadAsStringAsync().Result;
			try
			{
				loginotpResposne = JsonConvert.DeserializeObject<LoginotpResposne>(result);
			}
			catch
			{
				Term.WriteLine("请求失败" + result);
				throw new ModPcFatalException("程序终止：登录流程中断");
			}
			if (loginotpResposne.code != 0)
			{
				Term.WriteLine("登录失败，请检查cookie：" + result);
				throw new ModPcFatalException("程序终止：登录流程中断");
			}
			string authenticationOtpJson = app.GetAuthenticationOtpJson("1.14.6.45947", loginotpResposne.entity.aid, loginotpResposne.entity.otp_token, Login_otp);
			Function.ClientLog("[Login] Login request: " + authenticationOtpJson);
			ByteArrayContent byteArrayContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication-otp", authenticationOtpJson, out keyStr));
			byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			string text = CoreNative.ParseLoginResponse(client.PostAsync(app.Temp.url + "/authentication-otp", byteArrayContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			try
			{
				AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(text);
				UID = authenticationResponse.entity.entity_id;
				Function.ClientLog("[Login] Login UID:" + UID);
				LoginDToken = authenticationResponse.entity.token;
				Function.ClientLog("[Login] Login Dtoken:" + LoginDToken);
				LoginSRCToken = MclNetClient.GetDecryptToken(LoginDToken);
				Function.ClientLog("[Login] Login token:" + LoginSRCToken);
				sd.i("RELEASE", uint.Parse(UID));
				PEUID = JsonConvert.DeserializeObject<PEUid>(x19post("https://x19apigatewayobt.nie.netease.com", "/user-official-account-info/check", "{\"entity_id\":\"" + UID + "\"}")).entity.entity_id;
			}
			catch (Exception ex)
			{
				Term.WriteLine("出错了？请切换账号后重试！");
				Term.WriteLine("错误信息：" + ex.ToString());
				Term.WriteLine("登录响应：" + Regex.Unescape(text));
				Close();
				Term.ReadKey();
			}
			Term.Clear();
			Term.WriteLine("登录成功！");
			GetName();
			Term.WriteLine("UID:" + UID);
			UpdateLogin();
			app.Temp.Version = Function.GetVersion();
			Function.delete = true;
		}
	}

	public static string GetLoginToken(string Login_otp)
	{
		Function.ClientLog("[Login] create Login type:x19");
		if (Thread != null)
		{
			Close();
		}
		if (Login_otp.StartsWith("SDK4399"))
		{
			Login_otp = FeverAuth.SDK4399ToSauth(Login_otp);
		}
		if (Login_otp != null)
		{
			string keyStr = string.Empty;
			LoginotpResposne loginotpResposne = new LoginotpResposne();
			new AuthenticationResponse();
			if (Login_otp == "")
			{
				Term.WriteLine("未检测到cookie");
			}
			StringContent content = new StringContent(Login_otp, Encoding.UTF8, "application/json");
			string result = client.PostAsync(app.Temp.url + "/login-otp", content).Result.Content.ReadAsStringAsync().Result;
			try
			{
				loginotpResposne = JsonConvert.DeserializeObject<LoginotpResposne>(result);
			}
			catch
			{
				Term.WriteLine("请求失败" + result);
			}
			if (loginotpResposne.code != 0)
			{
				Term.WriteLine("登录失败，请检查cookie：" + result);
			}
			string authenticationOtpJson = app.GetAuthenticationOtpJson("1.14.6.45947", loginotpResposne.entity.aid, loginotpResposne.entity.otp_token, Login_otp);
			Function.ClientLog("[Login] Login request: " + authenticationOtpJson);
			ByteArrayContent byteArrayContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication-otp", authenticationOtpJson, out keyStr));
			byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			string result2 = CoreNative.HttpDecrypt(client.PostAsync(app.Temp.url + "/authentication-otp", byteArrayContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			Term.WriteLine("登录成功！");
			Term.WriteLine("UID:" + UID);
			return result2;
		}
		return null;
	}

	public static void PE_Login(string sauth)
	{
		if (Thread != null)
		{
			Close();
		}
		if (sauth.StartsWith("SDK4399"))
		{
			sauth = FeverAuth.SDK4399ToSauth(sauth);
		}
		if (sauth != null)
		{
			string guid = Function.GetGuid();
			Function.ClientLog("[Login] create Login type:g79 guid: " + guid);
			string text = app.Temp.engineVersion + app.Temp.libminecraftpe + app.Temp.patchVersion + app.Temp.patch + guid;
			Function.ClientLog("[Login] Login message: " + text);
			string text2 = Function.Base64Encode(CoreSign.PESignCount(text));
			Function.ClientLog("[Login] Login sign: " + text2);
			string text3 = JsonConvert.SerializeObject(new PEAURequest
			{
				sa_data = app.ReadJson("sa_data_pe.json"),
				engine_version = app.Temp.engineVersion,
				patch_version = app.Temp.patchVersion,
				message = text,
				sauth_json = JsonConvert.DeserializeObject<JObject>(sauth)["sauth_json"],
				seed = guid,
				sign = text2
			});
			Function.ClientLog("[Login] Login request: " + text3);
			StringContent stringContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(text3)).ToHex(), Encoding.UTF8, "application/json");
			stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			string login_otp = x19Crypt.HttpDecrypt_g79v12_(client.PostAsync(app.Temp.PE_url + "/pe-authentication", stringContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
			UpdateLogin();
			LoadToken(login_otp);
		}
	}

	public static string PE_GetLoginResponse(string sauth)
	{
		if (sauth.StartsWith("SDK4399"))
		{
			sauth = FeverAuth.SDK4399ToSauth(sauth);
		}
		if (sauth != null)
		{
			string guid = Function.GetGuid();
			Function.ClientLog("[Login] create Login type:g79 guid: " + guid);
			string text = app.Temp.engineVersion + app.Temp.libminecraftpe + app.Temp.patchVersion + app.Temp.patch + guid;
			Function.ClientLog("[Login] Login message: " + text);
			string text2 = Function.Base64Encode(CoreSign.PESignCount(text));
			Function.ClientLog("[Login] Login sign: " + text2);
			string text3 = JsonConvert.SerializeObject(new PEAURequest
			{
				sa_data = app.ReadJson("sa_data_pe.json"),
				engine_version = app.Temp.engineVersion,
				patch_version = app.Temp.patchVersion,
				message = text,
				sauth_json = JsonConvert.DeserializeObject<JObject>(sauth)["sauth_json"],
				seed = guid,
				sign = text2
			});
			Function.ClientLog("[Login] Login request: " + text3);
			StringContent stringContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(text3)).ToHex(), Encoding.UTF8, "application/json");
			stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return x19Crypt.HttpDecrypt_g79v12_(client.PostAsync(app.Temp.PE_url + "/pe-authentication", stringContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
		}
		return null;
	}

	public static void LoadToken(string Login_otp)
	{
		new LoginotpResposne();
		new AuthenticationResponse();
		string text = CoreNative.ParseLoginResponse(CoreNative.HttpEncrypt("/authentication-otp", Login_otp, out var _), out var _);
		try
		{
			AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(text);
			UID = authenticationResponse.entity.entity_id;
			Function.ClientLog("[Login] Login UID:" + UID);
			LoginDToken = authenticationResponse.entity.token;
			Function.ClientLog("[Login] Login Dtoken:" + LoginDToken);
			LoginSRCToken = MclNetClient.GetDecryptToken(LoginDToken);
			Function.ClientLog("[Login] Login token:" + LoginSRCToken);
			sd.i("RELEASE", uint.Parse(UID));
			PEUID = JsonConvert.DeserializeObject<PEUid>(x19post("https://x19apigatewayobt.nie.netease.com", "/user-official-account-info/check", "{\"entity_id\":\"" + UID + "\"}")).entity.entity_id;
		}
		catch
		{
			Term.WriteLine("出错了？请切换账号后重试！");
			Term.WriteLine("错误信息：" + Regex.Unescape(text));
			Close();
			Term.ReadKey();
		}
		Term.Clear();
		Term.WriteLine("登录成功！");
		GetName();
		Term.WriteLine("UID:" + UID);
		Function.delete = true;
		app.Temp.Version = Function.GetVersion();
	}

	public static void UpdateLogin()
	{
		Function.ClientLog("[AuthUpdate] Create Update");
		Thread = new Thread(() =>
		{
			try
			{
				while (true)
				{
					Thread.Sleep(600000);
					string text = UpdateAuthotp(app.Temp.url);
					AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(text);
					UID = authenticationResponse.entity.entity_id;
					LoginDToken = authenticationResponse.entity.token;
					LoginSRCToken = MclNetClient.GetDecryptToken(LoginDToken);
					Function.ClientLog("[AuthUpdate] Response:" + text);
					Function.ClientLog("[AuthUpdate] SRCToken:" + LoginSRCToken);
					Function.UpdateToken();
				}
			}
			catch (Exception ex)
			{
				Term.WriteLine(ex.Message);
				Function.ClientError("[AuthUpdate] " + ex.Message);
			}
		});
		Thread.Start();
	}

	public static void Close()
	{
		Function.ClientLog("[AuthUpdate] Delete");
		Thread?.Abort();
		Thread = null;
		Delete();
	}

	public static string UpdateAuthotp(string url)
	{
		try
		{
			Function.ClientLog("[Authentication] Update");
			string keyStr = string.Empty;
			string authenticationUpdate = GetAuthenticationUpdate(UID);
			string value = CoreNative.ComputeDynamicToken("/authentication/update", authenticationUpdate);
			HttpContent httpContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication/update", authenticationUpdate, out keyStr));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			return CoreNative.ParseLoginResponse(client.PostAsync(url + "/authentication/update", httpContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string UpdateAuthotp(string url, string token)
	{
		try
		{
			Function.ClientLog("[Authentication] Update");
			string keyStr = string.Empty;
			string authenticationUpdate = GetAuthenticationUpdate(UID);
			string value = x19Crypt.ComputeDynamicToken("/authentication/update", authenticationUpdate, token);
			HttpContent httpContent = new ByteArrayContent(CoreNative.HttpEncrypt("/authentication/update", authenticationUpdate, out keyStr));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			return CoreNative.HttpDecrypt(client.PostAsync(url + "/authentication/update", httpContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string GetAuthenticationUpdate(string uid)
	{
		return JsonConvert.SerializeObject(new AuthenticationEntity
		{
			entity_id = uid
		});
	}

	public static void Delete()
	{
		string text = JsonConvert.SerializeObject(new AuthenticationDelete
		{
			user_id = UID
		});
		string value = CoreNative.ComputeDynamicToken("/authentication/delete", text);
		HttpContent httpContent = new StringContent(text);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		string result = client.PostAsync(app.Temp.url + "/authentication/delete", httpContent).Result.Content.ReadAsStringAsync().Result;
		Function.ClientLog("[Authentication] Delete:" + result);
		Function.delete = false;
	}

	public static void GetName()
	{
		string value = CoreNative.ComputeDynamicToken("/user-detail", "");
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		app.player_info = Regex.Unescape(client.PostAsync("https://x19apigatewayobt.nie.netease.com/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result);
	}

	public static void SetName(string Name)
	{
		postAPIPC("{\"name\":\"" + Name + "\",\"entity_id\":\"" + UID + "\"}", "/nickname-setting");
	}

	public static void SearchPE(string room_info)
	{
		ItemSearch(room_info);
		LobbySearch(room_info);
		RoomWithName(room_info);
	}

	public static void Search(string room_info)
	{
		postAPIPC("{\"room_name\":\"" + room_info + "\",\"res_id\":\"\",\"version\":\"" + app.Temp.Version + "\",\"offset\":0,\"length\":50}", "/online-lobby-room/query/search-by-name");
		postAPIPC("{\"item_type\":2,\"keyword\":\"" + room_info + "\",\"master_type_id\":\"0\",\"network_tag\":false,\"year\":0,\"sort_type\":1,\"order\":0,\"price_type\":0,\"is_has\":false,\"offset\":0,\"length\":24}", "/item/query/search-by-keyword");
	}

	private static void ItemSearch(string room_info)
	{
		postAPI("{\"length\":10,\"keyword\":\"" + room_info + "\",\"offset\":0,\"sort_type\":1,\"asc_flag\":1}", "/pe-item/query/search-lobby-res-by-keyword");
	}

	private static void LobbySearch(string room_info)
	{
		postAPImclobtPE("{\"keyword\":\"" + room_info + "\",\"length\":10,\"offset\":0,\"version\":\"" + app.Temp.Version + "\"}", "/online-lobby-room/query/search-by-name-v2");
	}

	private static void RoomWithName(string room_info)
	{
		postAPIMcltransfer("{\"name\":\"" + room_info + "\",\"uid\":" + UID + "}", "/room-with-name");
	}

	public static void RoomListPE(string res_id)
	{
		try
		{
			ListEntityPE listEntityPE = JsonConvert.DeserializeObject<ListEntityPE>(postAPI("{\"res_id\":\"" + res_id + "\",\"version\":\"" + app.Temp.Version + "\",\"with_friend\":true,\"offset\":0,\"length\":10}", "/online-lobby-room/query/list-room-by-res-id"));
			for (int i = 0; i < listEntityPE.entities.Length; i++)
			{
				Function.OutRoomListPE(listEntityPE.entities[i]);
			}
		}
		catch (Exception innerException)
		{
			throw new FormatException("无法获取列表", innerException);
		}
	}

	public static void RoomList(string res_id)
	{
		try
		{
			string value = x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/list-room-by-res-id", "{\"res_id\":\"" + res_id + "\",\"version\":\"" + app.Temp.Version + "\",\"with_friend\":true,\"offset\":0,\"length\":10}");
			ListEntity listEntity = new ListEntity();
			listEntity = JsonConvert.DeserializeObject<ListEntity>(value);
			for (int i = 0; i < listEntity.entities.Length; i++)
			{
				Function.OutRoomList(listEntity.entities[i]);
			}
		}
		catch (Exception innerException)
		{
			throw new FormatException("无法获取列表", innerException);
		}
	}

	public static void joined(string room_id)
	{
		if (JsonConvert.DeserializeObject<JObject>(postAPI("{\"room_id\":\"" + room_id + "\",\"password\":\"\",\"check_visibilily\":true}", "/online-lobby-room-enter"))["code"].ToString() == "0")
		{
			room_id_public = room_id;
		}
	}

	public static void left(string roomid)
	{
		postAPIPC("{\"room_id\":\"" + roomid + "\"}", "/online-lobby-room-enter/leave-room");
	}

	public static void GetIP()
	{
		postAPI("", "/online-lobby-game-enter");
	}

	public static void PurchaseItem(string itemid)
	{
		postAPImclobtPE("{\"item_id\": \"" + itemid + "\", \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, \"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + itemid + "\", \"coupon_ids\": [], \"cdk_code\": \"\"}", "/pe-purchase-item/");
	}

	public static void PurchaseItemPC(string itemid)
	{
		postAPIPC("{\"entity_id\":0,\"item_id\":\"" + itemid + "\",\"item_level\":0,\"user_id\":\"" + UID + "\",\"purchase_time\":0,\"last_play_time\":0,\"total_play_time\":0,\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\",\"coupon_ids\":null,\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}", "/user-item-purchase");
	}

	public static void aicommand(string rid, string command)
	{
		postAPI("{\"type\": \"game_ai_command_prompt\",\"data\": {\"room\": \"" + rid + "\",\"game_type\": \"联机大厅\",\"content\": \"" + command + "\",\"send_error\": \"\",\"send_success\": true,\"type\": \"打字\"}}", "/salog");
		postAPI("{\"type\": \"game_ai_command_common_send\", \"data\": {\"content\": \"" + command + "\", \"game_type\": \"联机大厅\", \"room\": \"" + rid + "\", \"correct\": false}}", "/salog");
	}

	public static void NameUpdate(string name)
	{
		postAPIPC("{\"name\":\"" + name + "\"}", "/pc-nickname-setting/update");
		GetName();
		Function.GetName(app.player_info);
	}

	public static void SearchItem(string itemid)
	{
		postAPIPC("{\"item_id_list\":[\"" + itemid + "\"],\"channel_id\":11}", "/item-channel/query/search-item-channel-list-by-id");
		postAPIPC("{\"item_ids\":[\"" + itemid + "\"],\"entity_ids\":[]}", "/item/query/search-mcgame-item-list");
	}

	public static void GetGame()
	{
		postAPIPC("{\"os\":\"10.0\",\"version\":100000000,\"entity_id\":null}", "/cpp-game-client-info");
	}

	public static string postAPI(string requests, string url, bool output = true)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			Term.WriteLine("https://g79apigatewayobt.minecraft.cn" + url);
			string text = Regex.Unescape(client.PostAsync("https://g79apigatewayobt.minecraft.cn" + url, httpContent).Result.Content.ReadAsStringAsync().Result);
			if (output)
			{
				Term.WriteLine(text);
			}
			return text;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string postAPIPC(string requests, string url, bool displaylog = true)
	{
		try
		{
			Function.ClientLog("[ApiGateway] Url:" + url);
			Function.ClientLog("[ApiGateway] Request:" + requests);
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string result = client.PostAsync("https://x19apigatewayobt.nie.netease.com" + url, httpContent).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[ApiGateway] Response:" + result);
			if (displaylog)
			{
				Term.WriteLine(Regex.Unescape(result));
			}
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string postAPIPC_NoCommon(string requests, string url, string token, string uid, bool DisplayLog = true)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, token);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", uid);
			string text = Regex.Unescape(client.PostAsync("https://x19apigatewayobt.nie.netease.com" + url, httpContent).Result.Content.ReadAsStringAsync().Result);
			if (DisplayLog)
			{
				Term.WriteLine(text);
			}
			return text;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			return null;
		}
	}

	public static string postAPIHost(string requests, string url, string host)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, requests);
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

	public static string postAPIHost(string requests, string url, string host, string uid, string token)
	{
		try
		{
			string value = x19Crypt.ComputeDynamicToken(url, requests, token);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", uid);
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

	public static string x19post(string host, string url, string data)
	{
		try
		{
			Function.ClientLog("[ApiGateway] Url:" + url);
			Function.ClientLog("[ApiGateway] Request:" + data);
			string result = new HttpClient
			{
				DefaultRequestHeaders = 
				{
					{ "user-id", UID },
					{
						"user-token",
						CoreNative.ComputeDynamicToken(url, data)
					}
				}
			}.PostAsync(content: new StringContent(data, Encoding.UTF8, "application/json"), requestUri: host + url).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[ApiGateway] Response:" + result);
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string x19post(string host, string url, string data, byte[] MD5token, string uid)
	{
		try
		{
			Function.ClientLog("[ApiGateway] Url:" + url);
			Function.ClientLog("[ApiGateway] Request:" + data);
			string result = new HttpClient
			{
				DefaultRequestHeaders = 
				{
					{ "user-id", uid },
					{
						"user-token",
						x19Crypt.ComputeDynamicToken(url, data, MD5token)
					}
				}
			}.PostAsync(content: new StringContent(data, Encoding.UTF8, "application/json"), requestUri: host + url).Result.Content.ReadAsStringAsync().Result;
			Function.ClientLog("[ApiGateway] Response:" + result);
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static void postAPImclobtPE(string requests, string url)
	{
		try
		{
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			Term.WriteLine(Regex.Unescape(client.PostAsync("https://g79mclobt.minecraft.cn" + url, httpContent).Result.Content.ReadAsStringAsync().Result));
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
		}
	}

	public static void postAPIMcltransfer(string requests, string url)
	{
		try
		{
			HttpContent httpContent = new StringContent(requests);
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", CoreNative.ComputeDynamicToken(url, requests));
			httpContent.Headers.Add("user-id", UID);
			Term.WriteLine(Regex.Unescape(client.PostAsync("https://g79mcltransfer.minecraft.cn" + url, httpContent).Result.Content.ReadAsStringAsync().Result));
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
		}
	}

	public static string EncyptPOST(string url, string requests, string host)
	{
		try
		{
			Function.ClientLog("[CommonEncrypt] Url:" + url);
			Function.ClientLog("[CommonEncrypt] Request:" + requests);
			string keyStr = string.Empty;
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new ByteArrayContent(CoreNative.HttpEncrypt(url, requests, out keyStr));
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = CoreNative.HttpDecrypt(client.PostAsync(host + url, httpContent).Result.Content.ReadAsByteArrayAsync().Result, out keyStr);
			Term.WriteLine(Regex.Unescape(text));
			Function.ClientLog("[CommonEncrypt] Response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string EncyptPOST_g79v12(string url, string requests, string host)
	{
		try
		{
			Function.ClientLog("[CommonEncrypt] Url:" + url);
			Function.ClientLog("[CommonEncrypt] Request:" + requests);
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(requests)).ToHex());
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = x19Crypt.HttpDecrypt_g79v12(client.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
			Term.WriteLine(text);
			Function.ClientLog("[CommonEncrypt] Response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string EncyptPOST_g79v3(string url, string requests, string host)
	{
		try
		{
			Function.ClientLog("[CommonEncrypt] Url:" + url);
			Function.ClientLog("[CommonEncrypt] Request:" + requests);
			string value = CoreNative.ComputeDynamicToken(url, requests);
			HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v3(Encoding.UTF8.GetBytes(requests)).ToHex());
			httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			httpContent.Headers.Add("user-token", value);
			httpContent.Headers.Add("user-id", UID);
			string text = x19Crypt.ParseLoginResponse_g79v3(client.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
			Term.WriteLine(Regex.Unescape(text));
			Function.ClientLog("[CommonEncrypt] Response:" + text);
			return text;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static string getAPIPC(string url, bool output = true)
	{
		try
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://x19apigatewayobt.nie.netease.com" + url);
			string value = CoreNative.ComputeDynamicToken(url, "");
			httpWebRequest.Headers["user-id"] = UID;
			httpWebRequest.Headers["user-token"] = value;
			string str = new StreamReader(((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd();
			if (output)
			{
				Term.WriteLine(Regex.Unescape(str));
			}
			response_data = str;
			istrue = false;
			return response_data;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static void KickAsync(string uid)
	{
		new Thread(() =>
		{
			postAPIPC("{\"room_id\":\"" + room_id_public + "\",\"user_id\":" + uid + "}", "/online-lobby-member-kick");
		}).Start();
	}

	public static void Result()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Term.ForegroundColor = ConsoleColor.White;
		Term.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string text2 = "";
		text2 = ((!(request.HttpMethod == "POST") || request.InputStream == null) ? "1" : HandleRequest(request, response, EncryptionAuthentication));
		byte[] bytes = Encoding.UTF8.GetBytes(text2);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Term.ForegroundColor = ConsoleColor.Red;
			Term.WriteLine("网络蹦了：" + ex.ToString());
		}
		Term.ForegroundColor = ConsoleColor.Yellow;
		Term.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Term.ForegroundColor = ConsoleColor.Gray;
	}

	public static void Result_v2()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Term.ForegroundColor = ConsoleColor.White;
		Term.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string s = "";
		if (request.HttpMethod == "POST" && request.InputStream != null)
		{
			if (request.RawUrl == "/api/phoenix/login")
			{
				s = HandleRequest_v2(request, response, EncryptionAuthentication);
			}
			if (request.RawUrl == "/api/phoenix/transfer_check_num")
			{
				Term.ReadKey();
			}
		}
		else
		{
			s = "1";
		}
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Term.ForegroundColor = ConsoleColor.Red;
			Term.WriteLine("网络蹦了：" + ex.ToString());
		}
		Term.ForegroundColor = ConsoleColor.Yellow;
		Term.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Term.ForegroundColor = ConsoleColor.Gray;
	}

	public static void Result_v3()
	{
		HttpListenerContext context = httpobj.GetContext();
		HttpListenerRequest request = context.Request;
		HttpListenerResponse response = context.Response;
		string text = Guid.NewGuid().ToString();
		Term.ForegroundColor = ConsoleColor.White;
		Term.WriteLine("接到新的请求:" + text + ",时间：" + DateTime.Now.ToString() + " Url:" + request.RawUrl);
		context.Response.ContentType = "text/plain;charset=UTF-8";
		context.Response.AddHeader("Content-type", "text/plain");
		context.Response.ContentEncoding = Encoding.UTF8;
		string s = "";
		if (request.HttpMethod == "POST" && request.InputStream != null)
		{
			if (request.RawUrl == "/authentication")
			{
				s = HandleRequest_v3(request, response, EncryptionAuthentication);
			}
		}
		else
		{
			s = "1";
		}
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		try
		{
			using Stream stream = response.OutputStream;
			stream.Write(bytes, 0, bytes.Length);
		}
		catch (Exception ex)
		{
			Term.ForegroundColor = ConsoleColor.Red;
			Term.WriteLine("网络蹦了：" + ex.ToString());
		}
		Term.ForegroundColor = ConsoleColor.Yellow;
		Term.WriteLine("请求处理完成：" + text + ",时间：" + DateTime.Now.ToString() + "\r\n");
		Term.ForegroundColor = ConsoleColor.Gray;
	}

	private static string HandleRequest(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		string array2;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			if (!LinkConnection.Is_Connected)
			{
				app.linkConnection = new LinkConnection();
			}
			string empty = string.Empty;
			empty = ((!Encrypt) ? text : x19Crypt.HttpDecrypt_g79v12(text.HexToBytes()));
			Term.WriteLine(empty);
			authenticationg79 authenticationg80 = new authenticationg79();
			JObject jObject = JsonConvert.DeserializeObject<JObject>(empty);
			authenticationg80.bit = "64";
			authenticationg80.clientKey = jObject["clientKey"].ToString();
			authenticationg80.displayName = app.displayName;
			authenticationg80.engineVersion = app.Temp.engineVersion;
			authenticationg80.netease_sid = server_id;
			authenticationg80.os_name = "android";
			authenticationg80.patchVersion = app.Temp.patchVersion;
			authenticationg80.uid = long.Parse(UID);
			string text3 = JsonConvert.SerializeObject(authenticationg80);
			string empty2 = string.Empty;
			empty2 = ((!Encrypt) ? "/authentication" : "/authentication-v2");
			CoreNative.ComputeDynamicToken(empty2, text3);
			text2 = ((!Encrypt) ? HttpPost(empty2, text3, "https://g79authobt.minecraft.cn", out array2) : HttpPost_v2(empty2, text3, "https://g79authobt.minecraft.cn", out array2));
			Term.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Term.ForegroundColor = ConsoleColor.Red;
			Term.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Term.ForegroundColor = ConsoleColor.Green;
		Term.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return array2;
	}

	private static string HandleRequest_v2(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			Term.WriteLine(text);
			string clientKey = JsonConvert.DeserializeObject<JObject>(text)["client_public_key"].ToString();
			string text3 = JsonConvert.SerializeObject(new authenticationx19
			{
				bit = "32",
				clientKey = clientKey,
				displayName = app.displayName,
				engineVersion = app.Temp.engineVersion,
				netease_sid = server_id,
				os_name = "windows",
				patchVersion = "",
				pcCheck = "0",
				platform = "pc",
				uid = uint.Parse(UID)
			});
			_ = string.Empty;
			CoreNative.ComputeDynamicToken("/authentication-v2", text3);
			text2 = HttpPost_v2("/authentication-v2", text3, "https://g79authobt.minecraft.cn", out var _);
			text2 = text2.Substring(0, text2.IndexOf("\"]}") + 3);
			text2 = JsonConvert.SerializeObject(new fbEntity
			{
				success = true,
				server_msg = "感谢您使用TLSP验证服务！",
				token = "",
				respond_to = "",
				growth_level = 14,
				ip_address = server_ip,
				chainInfo = text2
			});
			Term.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Term.ForegroundColor = ConsoleColor.Red;
			Term.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Term.ForegroundColor = ConsoleColor.Green;
		Term.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return text2;
	}

	private static string HandleRequest_v3(HttpListenerRequest request, HttpListenerResponse response, bool Encrypt)
	{
		string text = null;
		string text2 = null;
		try
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[2048];
			int num = 0;
			int num2 = 0;
			do
			{
				num = request.InputStream.Read(array, 0, array.Length);
				num2 += num;
				list.AddRange(array);
			}
			while (num != 0);
			text = Encoding.UTF8.GetString(list.ToArray(), 0, num2);
			if (!LinkConnection.Is_Connected)
			{
				app.linkConnection = new LinkConnection();
			}
			Term.WriteLine(text);
			string clientKey = JsonConvert.DeserializeObject<JObject>(text)["identityPublicKey"].ToString();
			string param = JsonConvert.SerializeObject(new authenticationg79
			{
				bit = "64",
				clientKey = clientKey,
				displayName = app.displayName,
				engineVersion = app.Temp.engineVersion,
				netease_sid = server_id,
				os_name = "android",
				patchVersion = app.Temp.patchVersion,
				uid = uint.Parse(UID)
			});
			_ = string.Empty;
			text2 = HttpPost_v2("/authentication-v2", param, "https://g79authobt.minecraft.cn", out var _);
			text2 = text2.Substring(0, text2.IndexOf("\"]}") + 3);
			Term.WriteLine(text2);
		}
		catch (Exception ex)
		{
			response.StatusDescription = "403";
			response.StatusCode = 403;
			Term.ForegroundColor = ConsoleColor.Red;
			Term.WriteLine("在接收数据时发生错误:" + ex.ToString());
			return "在接收数据时发生错误:" + ex.ToString();
		}
		response.StatusDescription = "200";
		response.StatusCode = 200;
		Term.ForegroundColor = ConsoleColor.Green;
		Term.WriteLine("接收数据完成:" + text.Trim() + ",时间：" + DateTime.Now.ToString());
		return text2;
	}

	public static string HttpPost(string url, string param, string host, out string array)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(param);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("User-Agent", "libhttpclient/1.0.0.0");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return array = httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result;
	}

	public static string HttpPost_(string url, string param, string host)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(param);
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result;
	}

	public static string HttpPost_v2(string url, string param, string host, out string array)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(param)).ToHex());
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("User-Agent", "libhttpclient/1.0.0.0");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return x19Crypt.HttpDecrypt_g79v12((array = httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result).HexToBytes());
	}

	public static string HttpPost_v2_(string url, string param, string host)
	{
		HttpClient httpClient = new HttpClient();
		HttpContent httpContent = new StringContent(x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(param)).ToHex());
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", Encoding.ASCII.GetBytes(CoreNative.ComputeDynamicToken(url, param)).ToHex(toUpper: true));
		httpContent.Headers.Add("user-id", UID);
		httpClient.DefaultRequestHeaders.Add("accept-encoding", "gzip");
		httpClient.DefaultRequestHeaders.ExpectContinue = false;
		return x19Crypt.HttpDecrypt_g79v12(httpClient.PostAsync(host + url, httpContent).Result.Content.ReadAsStringAsync().Result.HexToBytes());
	}

	public static void Authserver(int port)
	{
		// 原版只试一个端口，多开或残留进程时直接抛 HttpListenerException。
		// 这里改成往后再试 9 个端口，都用不了才报错。
		for (int p = port; p < port + 10; p++)
		{
			try
			{
				HttpListener listener = new HttpListener();
				string text = "http://127.0.0.1:" + p + "/";
				listener.Prefixes.Add(text);
				listener.Start();
				httpobj = listener;
				AuthServerUrl = text;
				IsAuthServer = true;

				new Thread(() =>
				{
					while (true)
					{
						try
						{
							Result();
						}
						catch
						{
							Thread.Sleep(50);
						}
					}
				})
				{
					IsBackground = true
				}.Start();

				if (p != port)
				{
					Term.WriteLine("注意：端口 " + port + " 被占用，本地认证服改用 " + p);
				}
				Term.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
				return;
			}
			catch (Exception ex)
			{
				Term.WriteLine("[认证服] 端口 " + p + " 起不来：" + ex.Message);
			}
		}

		Term.WriteLine("✗ 本地认证服启动失败：" + port + "~" + (port + 9) + " 全被占用。");
		Term.WriteLine("  多半是还有别的 ES Tool 窗口开着（同一个端口只能给一个进程用）。");
		Term.WriteLine("  把所有 ES Tool 窗口关掉，再重新打开一个，然后重试。");
	}

	/// <summary>关闭本地认证服，释放端口</summary>
	public static void CloseAuthserver()
	{
		try
		{
			if (httpobj != null)
			{
				if (httpobj.IsListening) httpobj.Stop();
				httpobj.Close();
			}
		}
		catch { }
		finally
		{
			httpobj = null;
			IsAuthServer = false;
			AuthServerUrl = null;
		}
	}
}
