using System;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Login.NetEase.RPCServer;
using Login.loginauth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Utilities;
using text;

namespace Login.NetEase.DummyService;

internal class TraceUIDLockServer
{
	public static string LockPwd;

	private static Thread LockThread;

	private static Thread t;

	private static AuthenticationResponse authenticationResponse;

	private static int _processID;

	private static void RecvPid(byte[] array)
	{
		_processID = BitConverter.ToInt32(array, 0);
		Term.WriteLine("已获取pid" + _processID);
	}

	public static void TraceUser(string uid)
	{
		t = new Thread(() =>
		{
			app.start_stop = true;
			_ = string.Empty;
			string room_id = string.Empty;
			string item_id = string.Empty;
			string text = "0";
			bool flag = true;
			while (app.start_stop)
			{
				string playerDisplayName = app.GetPlayerDisplayName(uid);
				Function.ClientLog("Search name:" + playerDisplayName);
				string value = Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/search-by-name-v2", "{\"keyword\":\"" + playerDisplayName + "\",\"length\":10,\"offset\":0,\"version\":\"" + app.Temp.Version + "\"}");
				try
				{
					JObject jObject = JsonConvert.DeserializeObject<JObject>(value);
					room_id = jObject["entities"][0]["entity_id"].ToString();
					item_id = jObject["entities"][0]["res_id"].ToString();
					if (flag || text != room_id)
					{
						if (!flag)
						{
							try
							{
								LockThread.Abort();
							}
							catch
							{
							}
							try
							{
								t.Abort();
							}
							catch
							{
							}
						}
						LockThread = new Thread(() =>
						{
							while (app.start_stop)
							{
								Function.ClientLog("Starting.");
								Random random = new Random();
								string text2 = DummySrv.strings[random.Next(0, DummySrv.strings.Count)];
								DummySrv.strings.Remove(text2);
								string text3 = app.ReadJson(text2);
								Function.ClientLog("[Random][SAUTH]" + text3);
								try
								{
									authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(Http.GetLoginToken(text3));
									string name = Function.GetName(authenticationResponse.entity.entity_id, authenticationResponse.entity.token);
									Function.ClientLog("[Login] display name:" + name);
									Function.ClientLog("[Login] purchase item");
									Http.postAPIPC_NoCommon("{\"entity_id\":0,\"item_id\":\"" + item_id + "\",\"item_level\":0,\"user_id\":\"739563429\",\"purchase_time\":0,\"last_play_time\":0,\"total_play_time\":0,\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\",\"coupon_ids\":null,\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}", "/user-item-purchase", authenticationResponse.entity.token, authenticationResponse.entity.entity_id, DisplayLog: false);
									Thread.Sleep(1000);
									string value2 = Http.postAPIPC_NoCommon("{\"room_id\":\"" + room_id + "\",\"password\":\"" + LockPwd + "\",\"check_visibilily\":false}", "/online-lobby-room-enter", authenticationResponse.entity.token, authenticationResponse.entity.entity_id, DisplayLog: false);
									Function.ClientLog("[Login] joined room");
									JObject jObject2 = JsonConvert.DeserializeObject<JObject>(value2);
									if (int.Parse(jObject2["code"].ToString()) != 0)
									{
										Function.ClientError("The room could not be found");
										try
										{
											LockThread.Abort();
										}
										catch
										{
										}
										try
										{
											t.Abort();
										}
										catch
										{
										}
									}
									value2 = Http.postAPIPC_NoCommon("", "/online-lobby-game-enter", authenticationResponse.entity.token, authenticationResponse.entity.entity_id, DisplayLog: false);
									Function.ClientLog("[Login] get server host");
									Http.postAPIPC_NoCommon("{\"room_id\":\"" + room_id + "\"}", "/online-lobby-room-enter/leave-room", authenticationResponse.entity.token, authenticationResponse.entity.entity_id, DisplayLog: false);
									Function.ClientLog("[Login] leave room");
									bool flag2 = false;
									string text4 = string.Empty;
									string text5 = string.Empty;
									try
									{
										jObject2 = JsonConvert.DeserializeObject<JObject>(value2);
										text4 = jObject2["entity"]["server_host"].ToString();
										text5 = jObject2["entity"]["server_port"].ToString();
										Function.ClientLog("[Login] server host:" + text4 + ":" + text5);
									}
									catch
									{
										Function.ClientError("[Login] get server host:" + jObject2["message"].ToString());
										flag2 = true;
									}
									if (!flag2)
									{
										RPCPort rPCPort = new RPCPort();
										rPCPort.RegisterReceiveCallBack(0, RecvPid);
										Function.ClientLog("[CreateConn]Port:" + rPCPort.LauncherControlPort);
										app.InvokeCmd("start FastBuilder.exe --uid " + authenticationResponse.entity.entity_id + " --sid 4654415063569776560:LobbyGame --name " + name + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + text4 + " --port " + text5 + " --token " + Convert.ToBase64String(HashHelper.SafeCompleteMD5(authenticationResponse.entity.token)) + " --typ bf --LauncherPort " + rPCPort.LauncherControlPort);
										Function.ClientLog("[Login] invoke FastBuilder");
										DummySrv.strings.Add(text2);
										Function.ClientLog("[Base] Free Sauth");
									}
									else
									{
										DummySrv.strings.Add(text2);
										Function.ClientLog("[Base] Free Sauth");
									}
								}
								catch (Exception ex)
								{
									Function.ClientError(ex.ToString());
									DummySrv.strings.Add(text2);
									Function.ClientLog("[Base] Free Sauth");
								}
							}
						});
						LockThread.Start();
					}
				}
				catch
				{
				}
				flag = false;
				text = room_id;
				Thread.Sleep(1000);
			}
			try
			{
				LockThread.Abort();
			}
			catch
			{
			}
		});
		t.Start();
	}
}
