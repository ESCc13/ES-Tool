using System;
using System.Threading;
using Login.NetEase.DummyService.CoreLoginUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Utilities;
using text;

namespace Login.NetEase.DummyService;

internal class FLockServer
{
	public static bool DisplayWindow;

	private x19Login x19Login;

	public string uid;

	public string serverid;

	private string cookiepath;

	private static Thread Thread;

	private bool stop = true;

	public FLockServer(string cookiepath, string serverid, string password, string uid)
	{
		Thread = new Thread(() =>
		{
			try
			{
				this.cookiepath = cookiepath;
				this.serverid = serverid;
				this.uid = uid;
				x19Login = new x19Login();
				if (x19Login.PELogin(app.ReadJson(cookiepath)) != 0)
				{
					DummySrv.LockClients.Remove(this);
					Thread.Sleep(2000);
					app.chatConnection.SendUIDMessage("启动锁服时发生错误：" + x19Login.AuthenticationEntity.message, uid);
					Close();
					return;
				}
				JObject jObject = JsonConvert.DeserializeObject<JObject>(x19Login.x19POST("https://g79mclobt.minecraft.cn", "{\"server_name\": \"" + serverid + "\", \"offset\": 0}", "/rental-server/query/search-by-name"));
				string text;
				try
				{
					text = jObject["entities"][0]["entity_id"].ToString();
				}
				catch
				{
					throw new Exception("未能搜索到服务器！");
				}
				if (jObject["entities"][0]["name"].ToString() != serverid)
				{
					throw new Exception("未能搜索到服务器！");
				}
				while (true)
				{
					jObject = JsonConvert.DeserializeObject<JObject>(x19Login.x19POST(LockServer.WebServerUrl, "{\"server_id\": \"" + text + "\", \"pwd\": \"" + password + "\"}", "/rental-server-world-enter/get"));
					if (int.Parse(jObject["code"].ToString()) != 0)
					{
						Thread.Sleep(1000);
					}
					else
					{
						string text2 = jObject["entity"]["mcserver_host"].ToString();
						string text3 = jObject["entity"]["mcserver_port"].ToString();
						if (DisplayWindow)
						{
							app.InvokeCmd("start FastBuilder.exe --uid " + x19Login.UID + " --sid " + text + ":RentalGame --name " + Function.GetDisplayName(x19Login.player_info) + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --patchVersion " + app.Temp.patchVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + text2 + " --port " + text3 + " --typ bf --g79 --token " + Convert.ToBase64String(HashHelper.SafeCompleteMD5(x19Login.LoginSRCToken)));
						}
						else
						{
							app.InvokeCmd("FastBuilder.exe --uid " + x19Login.UID + " --sid " + text + ":RentalGame --name " + Function.GetDisplayName(x19Login.player_info) + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --patchVersion " + app.Temp.engineVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + text2 + " --port " + text3 + " --typ bf --g79 --token " + Convert.ToBase64String(HashHelper.SafeCompleteMD5(x19Login.LoginSRCToken)));
						}
					}
				}
			}
			catch (Exception)
			{
				if (stop)
				{
					Close();
					Thread.Sleep(2000);
					app.chatConnection.SendUIDMessage("启动锁服时发生错误：" + x19Login.AuthenticationEntity.message, uid);
				}
			}
		});
		Thread.Start();
	}

	public void Close()
	{
		try
		{
			stop = false;
			DummySrv.strings.Add(cookiepath);
			x19Login.Close();
			DummySrv.LockClients.Remove(this);
			try
			{
				Thread.Abort();
			}
			catch
			{
			}
		}
		catch
		{
		}
	}
}
