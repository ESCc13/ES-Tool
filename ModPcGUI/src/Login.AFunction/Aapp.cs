using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Login.NetEase;
using Login.NetEase.ChatService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using text;

namespace Login.AFunction;

internal class Aapp
{
	public void Arun(string json)
	{
		Http.room_id_public = app.ReadJson("roomkiller.log");
		ValuesJson valuesJson = new ValuesJson();
		try
		{
			valuesJson = JsonConvert.DeserializeObject<ValuesJson>(json);
			Http.Login(app.ReadJson(valuesJson.user_info));
			app.start_stop = true;
			string room_id = valuesJson.room_id;
			Http.istrue = false;
			string value = Http.postAPIPC("{\"room_name\":\"" + room_id + "\",\"res_id\":\"\",\"version\":\"" + app.Temp.Version + "\",\"offset\":0,\"length\":50}", "/online-lobby-room/query/search-by-name");
			Thread.Sleep(1000);
			try
			{
				Http.postAPIPC("{\"room_id\":\"" + Http.room_id_public + "\"}", "/online-lobby-room-enter/leave-room");
			}
			catch (Exception ex)
			{
				Term.WriteLine(ex.ToString());
			}
			JObject jObject = JsonConvert.DeserializeObject<JObject>(value);
			jObject = JsonConvert.DeserializeObject<JObject>(jObject["entities"][0].ToString());
			Http.PurchaseItemPC(jObject["res_id"].ToString());
			app.updateroom(jObject["entity_id"].ToString());
			if (valuesJson.joined)
			{
				app.updateroom(room_id);
			}
			File.WriteAllText("roomkiller.log", jObject["entity_id"].ToString());
			app.Ajoin(jObject["entity_id"].ToString(), valuesJson.password);
			app.Command();
		}
		catch (Exception ex2)
		{
			Term.WriteLine("未找到目标房间：" + ex2.Message);
		}
	}

	public void ArunUpdate(string json)
	{
		try
		{
			Http.Login(app.ReadJson(json));
			Function.AUpdateLogin();
			app.Command();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
		}
	}

	public void Arun_rid(string json)
	{
		Http.room_id_public = app.ReadJson("roomkiller.log");
		ValuesJson valuesJson = new ValuesJson();
		try
		{
			valuesJson = JsonConvert.DeserializeObject<ValuesJson>(json);
			Http.Login(app.ReadJson(valuesJson.user_info));
			app.start_stop = true;
			string room_id = valuesJson.room_id;
			Http.istrue = false;
			if (valuesJson.joined)
			{
				app.updateroom(room_id);
			}
			try
			{
				Http.postAPIPC("{\"room_id\":\"" + Http.room_id_public + "\"}", "/online-lobby-room-enter/leave-room");
			}
			catch (Exception ex)
			{
				Term.WriteLine(ex.ToString());
			}
			File.WriteAllText("roomkiller.log", valuesJson.room_id);
			app.Ajoin(room_id, valuesJson.password);
			app.Command();
		}
		catch (Exception ex2)
		{
			Term.WriteLine(ex2.Message);
		}
	}

	public void ServerRun(string text_value)
	{
		try
		{
			LobbyValue lobbyValue = JsonConvert.DeserializeObject<LobbyValue>(text_value);
			Http.Login(app.ReadJson(lobbyValue.user_info));
			Http.GetName();
			Http.PurchaseItemPC(lobbyValue.item_ids);
			Thread.Sleep(1000);
			if (lobbyValue.load)
			{
				try
				{
					JObject jObject = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.getAPIPC("/online-lobby-backup/query/list-by-user"))["entities"][0].ToString());
					string text = jObject["res_id"].ToString();
					string text2 = jObject["save_id"].ToString();
					app.updateroom(Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"room_name\":\"" + Function.GetName(app.player_info) + "\",\"max_count\":" + lobbyValue.player_max + ",\"visibility\":0,\"res_id\":\"" + text + "\",\"save_id\":\"" + text2 + "\",\"password\":\"" + lobbyValue.password + "\"}", "/online-lobby-room"))["entity"].ToString())["entity_id"].ToString());
					Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
					Term.WriteLine("服务器正在启动，输入getip获取服务器ip，详情输入help");
					app.Command();
					return;
				}
				catch
				{
					Term.WriteLine("错误：你没有保存存档！");
					return;
				}
			}
			app.updateroom(Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"room_name\":\"" + Function.GetName(app.player_info) + "\",\"max_count\":" + lobbyValue.player_max + ",\"visibility\":0,\"res_id\":\"" + lobbyValue.item_ids + "\",\"save_id\":\"\",\"password\":\"" + lobbyValue.password + "\"}", "/online-lobby-room"))["entity"].ToString())["entity_id"].ToString());
			Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
			Term.WriteLine("服务器正在启动，输入getip获取服务器ip，详情输入help");
			app.Command();
		}
		catch (Exception ex)
		{
			Term.WriteLine("创建房间时发生错误：" + ex.Message);
		}
	}

	public void SendUID(string json, string user, string message, int V)
	{
		try
		{
			Http.Login(app.ReadJson(user));
			app.start_stop = true;
			Http.istrue = false;
			Asenduid(json, app.ReadJson(message), V);
			app.Command();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
		}
	}

	public void Asenduid(string uid, string message, int V)
	{
		app.chatConnection = new ChatConnection();
		app.start_stop = true;
		new Thread(() =>
		{
			int i = 0;
			while (app.start_stop)
			{
				byte[] bytes = Encoding.UTF8.GetBytes("[" + uid + ",\"" + message + "\"]");
				byte[] array = new byte[bytes.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)1281), 0, array, 0, 2);
				Array.Copy(bytes, 0, array, 2, bytes.Length);
				for (; i < V; i++)
				{
					app.chatConnection.Encryption.Process(array);
					byte[] array2 = new byte[array.Length + 2];
					Array.Copy(BitConverter.GetBytes((ushort)array.Length), 0, array2, 0, 2);
					Array.Copy(array, 0, array2, 2, array.Length);
					byte[] array3 = new byte[array2.Length + 2];
					Array.Copy(BitConverter.GetBytes((ushort)array2.Length), 0, array3, 0, 2);
					Array.Copy(array2, 0, array3, 2, array2.Length);
					app.chatConnection.networkStream.Write(array3, 0, array3.Length);
					Term.WriteLine("成功发送");
					Thread.Sleep(2000);
				}
				i = 0;
			}
		}).Start();
	}

	public void Dummy(string user, string port, string ip, string serverid)
	{
		Http.Login(app.ReadJson(user));
		Function.GetName(app.player_info);
		startserver(port, ip, serverid);
		app.InvokeCmdAsync("phoenixbuilder-tlsp-windows-executable-x86_64.exe --auth-server=http://localhost:" + port);
		app.Command();
	}

	public void startserver(string port, string ip, string serverid)
	{
		try
		{
			Http.server_ip = ip;
			Http.server_id = serverid;
			Http.httpobj = new HttpListener();
			string text = "http://127.0.0.1:" + port + "/";
			Http.httpobj.Prefixes.Add(text);
			Http.httpobj.Start();
			new Thread(() =>
			{
				while (true)
				{
					Http.Result_v2();
				}
			}).Start();
			Http.AuthServerUrl = text;
			Term.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
		}
		catch (Exception ex)
		{
			Term.WriteLine("未经处理的异常：" + ex.ToString());
		}
	}
}
