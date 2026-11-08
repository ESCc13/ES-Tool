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

internal class FBClient
{
	private AuthenticationResponse authenticationResponse;

	private int _processID;

	public FBClient(string cookiePath, string rid, string uid, string pwd = "")
	{
		new Thread(() =>
		{
			try
			{
				string login_otp = app.ReadJson(cookiePath);
				authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(Http.GetLoginToken(login_otp));
				string name = GetName(authenticationResponse.entity.entity_id, authenticationResponse.entity.token);
				JObject jObject = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon("{\"room_name\":\"" + rid + "\",\"res_id\":\"\",\"version\":\"" + app.Temp.Version + "\",\"offset\":0,\"length\":1}", "/online-lobby-room/query/search-by-name", authenticationResponse.entity.token, authenticationResponse.entity.entity_id));
				string text = jObject["entities"][0]["entity_id"].ToString();
				string text2 = jObject["entities"][0]["res_id"].ToString();
				Http.postAPIPC_NoCommon("{\"entity_id\":0,\"item_id\":\"" + text2 + "\",\"item_level\":0,\"user_id\":\"739563429\",\"purchase_time\":0,\"last_play_time\":0,\"total_play_time\":0,\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\",\"coupon_ids\":null,\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}", "/user-item-purchase", authenticationResponse.entity.token, authenticationResponse.entity.entity_id);
				Thread.Sleep(1000);
				Http.postAPIPC_NoCommon("{\"room_id\":\"" + text + "\",\"password\":\"" + pwd + "\",\"check_visibilily\":false}", "/online-lobby-room-enter", authenticationResponse.entity.token, authenticationResponse.entity.entity_id);
				string value = Http.postAPIPC_NoCommon("", "/online-lobby-game-enter", authenticationResponse.entity.token, authenticationResponse.entity.entity_id);
				Http.postAPIPC_NoCommon("{\"room_id\":\"" + text + "\"}", "/online-lobby-room-enter/leave-room", authenticationResponse.entity.token, authenticationResponse.entity.entity_id);
				jObject = JsonConvert.DeserializeObject<JObject>(value);
				string ip = jObject["entity"]["server_host"].ToString();
				string port = jObject["entity"]["server_port"].ToString();
				bool a = true;
				Thread thread = new Thread(() =>
				{
					app.InvokeCmd("start FastBuilder.exe --uid " + authenticationResponse.entity.entity_id + " --sid 4654415063569776560:LobbyGame --name " + name + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + ip + " --port " + port + " --token " + Convert.ToBase64String(HashHelper.SafeCompleteMD5(authenticationResponse.entity.token)) + " --typ bf");
					DummySrv.strings.Add(cookiePath);
					a = false;
					app.chatConnection.SendUIDMessage("崩服成功！", uid);
				});
				thread.Start();
				Thread.Sleep(5000);
				if (a)
				{
					DummySrv.strings.Add(cookiePath);
					try
					{
						thread.Abort();
					}
					catch
					{
					}
					app.chatConnection.SendUIDMessage("连接超时！", uid);
				}
			}
			catch
			{
			}
		}).Start();
	}

	private void RecvPid(byte[] array)
	{
		_processID = BitConverter.ToInt32(array, 0);
		Term.WriteLine("已获取pid" + _processID);
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
