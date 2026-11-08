using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Login.loginauth;
using Mark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Utilities;
using text;

namespace Login.NetEase.DummyService;

internal class SpTask
{
	private AuthenticationResponse authenticationResponse;

	private int _processID;

	private void RecvPid(byte[] array)
	{
		_processID = BitConverter.ToInt32(array, 0);
		Term.WriteLine("已获取pid" + _processID);
	}

	public SpTask(string cookie, string rid, string pwd, string spfile)
	{
		try
		{
			authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(Http.GetLoginToken(cookie));
			string name = GetName(authenticationResponse.entity.entity_id, authenticationResponse.entity.token);
			Thread thread = new Thread(() =>
			{
				Thread.Sleep(60000);
				Http.UpdateAuthotp("https://x19obtcore.nie.netease.com:8443", authenticationResponse.entity.token);
			});
			thread.Start();
			string requests = "{\"room_name\":\"" + rid + "\",\"res_id\":\"\",\"version\":\"" + app.Temp.Version + "\",\"offset\":0,\"length\":1}";
			JObject jObject = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon(requests, "/online-lobby-room/query/search-by-name", authenticationResponse.entity.token, authenticationResponse.entity.entity_id));
			string text = jObject["entities"][0]["entity_id"].ToString();
			string text2 = jObject["entities"][0]["res_id"].ToString();
			requests = "{\"entity_id\":0,\"item_id\":\"" + text2 + "\",\"item_level\":0,\"user_id\":\"739563429\",\"purchase_time\":0,\"last_play_time\":0,\"total_play_time\":0,\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\",\"coupon_ids\":null,\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}";
			Http.postAPIPC_NoCommon(requests, "/user-item-purchase", authenticationResponse.entity.token, authenticationResponse.entity.entity_id);
			Thread.Sleep(1000);
			while (app.start_stop)
			{
				requests = "{\"room_id\":\"" + text + "\",\"password\":\"" + pwd + "\",\"check_visibilily\":false}";
				JObject jObject2;
				while (true)
				{
					jObject2 = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon(requests, "/online-lobby-room-enter", authenticationResponse.entity.token, authenticationResponse.entity.entity_id));
					if (jObject2["code"].ToString() == "12015")
					{
						throw new Exception();
					}
					if (jObject2["code"].ToString() == "12022")
					{
						Thread.Sleep(1000);
						continue;
					}
					if ((jObject2["code"].ToString() != "12002") | !app.start_stop)
					{
						break;
					}
					Thread.Sleep(1000);
				}
				while (true)
				{
					jObject = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon("", "/online-lobby-game-enter", authenticationResponse.entity.token, authenticationResponse.entity.entity_id));
					if (jObject2["code"].ToString() != "12031")
					{
						break;
					}
					Thread.Sleep(1000);
				}
				requests = "{\"room_id\":\"" + text + "\"}";
				Http.postAPIPC_NoCommon(requests, "/online-lobby-room-enter/leave-room", authenticationResponse.entity.token, authenticationResponse.entity.entity_id);
				string text3 = jObject["entity"]["server_host"].ToString();
				string text4 = jObject["entity"]["server_port"].ToString();
				app.InvokeCmd("start FastBuilder.exe --uid " + authenticationResponse.entity.entity_id + " --sid 4654415063569776560:LobbyGame --name " + name + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + text3 + " --port " + text4 + " --token " + Convert.ToBase64String(HashHelper.SafeCompleteMD5(authenticationResponse.entity.token)) + " --typ sp --spfile " + spfile);
			}
			try
			{
				thread.Abort();
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
		}
	}

	public string GetName(string UID, string token)
	{
		string value = x19Crypt.ComputeDynamicToken("/user-detail", "", token);
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		string result = Http.client.PostAsync("https://x19apigatewayobt.nie.netease.com/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result;
		if (Function.GetName(result) == "")
		{
			bool flag = false;
			string text;
			do
			{
				text = (flag ? Function.GetRandomKeyv2(8) : ("NO" + UID));
				flag = true;
			}
			while (!(JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon("{\"name\":\"" + text + "\",\"entity_id\":\"" + UID + "\"}", "/nickname-setting", authenticationResponse.entity.token, authenticationResponse.entity.entity_id))["code"].ToString() == "0"));
			return text;
		}
		return Function.GetName(result);
	}
}
