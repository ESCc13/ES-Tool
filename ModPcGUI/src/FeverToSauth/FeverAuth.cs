using System;
using System.Collections.Generic;
using System.Text;
using JS4399MC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FeverToSauth;

public class FeverAuth
{
	internal class SDK4399Token
	{
		public string username { get; set; }

		public string password { get; set; }
	}

	public static string FeverToSauth(string token)
	{
		FeverToken feverToken = JsonConvert.DeserializeObject<FeverToken>(Base64UnEncode(token));
		HttpHelper.InitHttp();
		JObject jObject = JsonConvert.DeserializeObject<JObject>(HttpHelper.postAPImclobtPE("https://service.mkey.163.com/mpay/api/users/create_ticket", "app_channel=netease.allysdk3rd&app_mode=2&app_type=games&arch=win_x64&cv=c4.2.0&device_id=" + feverToken.deviceid + "&game_id=aecglf6ee4aaaarz-g-a50&gv=1.17.0.0&mcount_app_key=EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq&mcount_transaction_id=6&process_id=3120&sv=10.0.19045&token=" + feverToken.sessionid + "&transid=4062C17975B3EDA2E328FF52C4F84D5F_1731819663617_100030765&uni_transaction_id=4062C17975B3EDA2E328FF52C4F84D5F_1731819695814_100030869&updater_cv=c1.0.0&user_id=" + feverToken.sdkuid));
		string text = jObject["ticket"].ToString();
		jObject = JsonConvert.DeserializeObject<JObject>(HttpHelper.postAPImclobtPE("https://service.mkey.163.com/mpay/api/users/login/ticket", "app_channel=a50_sdk_cn&app_mode=2&app_type=games&arch=win_x32&cv=c4.5.0&device_id=" + feverToken.deviceid + "&game_id=aecfrxodyqaaaajp-g-x19&gv=1.14.18.24399&mcount_app_key=EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq&mcount_transaction_id=9c4ae54f-a4de-11ef-8ba4-4b696ee8d88b-2&opt_fields=nickname%2Cavatar%2Crealname_status%2Cmobile_bind_status%2Cmask_related_mobile%2Crelated_login_status&process_id=3784&sv=10.0.19045&ticket=" + text + "&transid=4062C17975B3EDA2E328FF52C4F84D5F_1731846146081_100018943&uni_transaction_id=4062C17975B3EDA2E328FF52C4F84D5F_1731846146300_100018943&updater_cv=c1.0.0"));
		feverToken.sessionid = jObject["user"]["token"].ToString();
		sauth_json_c sauth_json_c2 = new sauth_json_c();
		sau sau2 = new sau();
		Aim value = new Aim
		{
			aim = jObject["user"]["pc_ext_info"]["src_client_ip"].ToString(),
			country = "CN",
			tz = "+0800"
		};
		sau2.gameid = "x19";
		sau2.login_channel = "netease";
		sau2.app_channel = "netease";
		sau2.platform = "pc";
		sau2.sdkuid = feverToken.sdkuid;
		sau2.sessionid = feverToken.sessionid;
		sau2.sdk_version = jObject["user"]["pc_ext_info"]["src_sdk_version"].ToString();
		sau2.udid = "4062C17975B3EDA2E328FF52C4F84D5F";
		sau2.deviceid = feverToken.deviceid;
		sau2.aim_info = JsonConvert.SerializeObject(value);
		sau2.client_login_sn = "F1A6560B3E6028C74A4616693EA21430";
		sau2.source_platform = "pc";
		sau2.ip = jObject["user"]["pc_ext_info"]["src_client_ip"].ToString();
		sauth_json_c2.sauth_json = JsonConvert.SerializeObject(sau2);
		return JsonConvert.SerializeObject(sauth_json_c2);
	}

	public static string Base64Encode(string plainText)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
	}

	public static string Base64UnEncode(string plainText)
	{
		byte[] bytes = Convert.FromBase64String(plainText);
		return Encoding.UTF8.GetString(bytes);
	}

	public static string SDK4399ToSauth(string token)
	{
		try
		{
			token = token.Substring(7);
			token = Base64UnEncode(token);
			SDK4399Token sDK4399Token = JsonConvert.DeserializeObject<SDK4399Token>(token);
			JS4399Result result = new JS4399(new JS4399HttpConfig()).JS4399LoginAsync(new Dictionary<string, object>
			{
				{ "username", sDK4399Token.username },
				{ "password", sDK4399Token.password }
			}, async (string base64) => Ocr.GetOcr(base64)).Result;
			if (result.Success)
			{
				return result.SauthJson;
			}
			Term.WriteLine(result.Message);
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
