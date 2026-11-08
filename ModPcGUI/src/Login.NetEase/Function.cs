using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppLogin.NetEase;
using Login.NetEase.RPCServer;
using Login.NetEase.Utils;
using MCStudio.Network;
using Mark;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFLauncher.Manager.Configuration.CppConfigure;
using WPFLauncher.Manager.LanGame;
using WPFLauncher.Network.Protocol.LobbyGame;
using WPFLauncher.Util;
using text;

namespace Login.NetEase;

internal class Function
{
	public static string IP;

	public static string Port;

	public static string Name;

	public static ArrayList al = new ArrayList();

	public static bool delete { get; set; }

	private static LobbyGameRoomEntity entity { get; set; } = new LobbyGameRoomEntity();

	public static string GetGuid()
	{
		return GetRandomKey(8) + "-" + GetRandomKey(4) + "-" + GetRandomKey(4) + "-" + GetRandomKey(4) + "-" + GetRandomKey(12);
	}

	public static void ClientLog(string log, ConsoleColor color = ConsoleColor.Gray)
	{
		if (Program.Client != null)
		{
			byte b = 7;
			switch (color)
			{
			case ConsoleColor.Black:
				b = 0;
				break;
			case ConsoleColor.DarkBlue:
				b = 1;
				break;
			case ConsoleColor.DarkGreen:
				b = 2;
				break;
			case ConsoleColor.DarkCyan:
				b = 3;
				break;
			case ConsoleColor.DarkRed:
				b = 4;
				break;
			case ConsoleColor.DarkMagenta:
				b = 5;
				break;
			case ConsoleColor.DarkYellow:
				b = 6;
				break;
			case ConsoleColor.Gray:
				b = 7;
				break;
			case ConsoleColor.DarkGray:
				b = 8;
				break;
			case ConsoleColor.Blue:
				b = 9;
				break;
			case ConsoleColor.Green:
				b = 10;
				break;
			case ConsoleColor.Cyan:
				b = 11;
				break;
			case ConsoleColor.Red:
				b = 12;
				break;
			case ConsoleColor.Magenta:
				b = 13;
				break;
			case ConsoleColor.Yellow:
				b = 14;
				break;
			case ConsoleColor.White:
				b = 15;
				break;
			}
			byte[] bytes = Encoding.UTF8.GetBytes("[INFO][" + DateTime.Now.ToString() + "]" + log);
			byte[] array = new byte[bytes.Length + 5];
			byte[] bytes2 = BitConverter.GetBytes(bytes.Length);
			Array.Copy(bytes, 0, array, 5, bytes.Length);
			Array.Copy(bytes2, 0, array, 0, 4);
			array[4] = b;
			Program.Writer.Write(array);
		}
		else if (app.Temp.connect_log)
		{
			Term.ForegroundColor = color;
			string value = "[INFO][" + DateTime.Now.ToString() + "]" + log;
			Term.ForegroundColor = ConsoleColor.Gray;
			Term.WriteLine(value);
		}
	}

	public static void ClientError(string log, ConsoleColor color = ConsoleColor.Red)
	{
		if (Program.Client != null)
		{
			byte b = 7;
			switch (color)
			{
			case ConsoleColor.Black:
				b = 0;
				break;
			case ConsoleColor.DarkBlue:
				b = 1;
				break;
			case ConsoleColor.DarkGreen:
				b = 2;
				break;
			case ConsoleColor.DarkCyan:
				b = 3;
				break;
			case ConsoleColor.DarkRed:
				b = 4;
				break;
			case ConsoleColor.DarkMagenta:
				b = 5;
				break;
			case ConsoleColor.DarkYellow:
				b = 6;
				break;
			case ConsoleColor.Gray:
				b = 7;
				break;
			case ConsoleColor.DarkGray:
				b = 8;
				break;
			case ConsoleColor.Blue:
				b = 9;
				break;
			case ConsoleColor.Green:
				b = 10;
				break;
			case ConsoleColor.Cyan:
				b = 11;
				break;
			case ConsoleColor.Red:
				b = 12;
				break;
			case ConsoleColor.Magenta:
				b = 13;
				break;
			case ConsoleColor.Yellow:
				b = 14;
				break;
			case ConsoleColor.White:
				b = 15;
				break;
			}
			byte[] bytes = Encoding.UTF8.GetBytes("[INFO][" + DateTime.Now.ToString() + "]" + log);
			byte[] array = new byte[bytes.Length + 5];
			byte[] bytes2 = BitConverter.GetBytes(bytes.Length);
			Array.Copy(bytes, 0, array, 5, bytes.Length);
			Array.Copy(bytes2, 0, array, 0, 4);
			array[4] = b;
			Program.Writer.Write(array);
		}
		else if (app.Temp.connect_log)
		{
			Term.ForegroundColor = color;
			string value = "[ERROR][" + DateTime.Now.ToString() + "]" + log;
			Term.ForegroundColor = ConsoleColor.Gray;
			Term.WriteLine(value);
		}
	}

	public static string GetName(string UID, string token)
	{
		string value = x19Crypt.ComputeDynamicToken("/user-detail", "", token);
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		string result = Http.client.PostAsync("https://x19apigatewayobt.nie.netease.com/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result;
		if (GetName(result) == "")
		{
			string randomKeyv;
			do
			{
				randomKeyv = GetRandomKeyv2(8);
			}
			while (!(JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon("{\"name\":\"" + randomKeyv + "\",\"entity_id\":\"" + UID + "\"}", "/nickname-setting", token, UID))["code"].ToString() == "0"));
			return randomKeyv;
		}
		return GetName(result);
	}

	public static string GetName(string values)
	{
		try
		{
			app.displayName = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(values)["entity"].ToString())["name"].ToString();
			return app.displayName;
		}
		catch
		{
			return null;
		}
	}

	public static string GetDisplayName(string values)
	{
		try
		{
			return JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(values)["entity"].ToString())["name"].ToString();
		}
		catch
		{
			return null;
		}
	}

	public static void StartCheck(byte[] bytes)
	{
		byte[] message = SimplePack.Pack((ushort)1031, IP, int.Parse(Port), Name, false);
		app.GameRpcPort.SendControlData(message);
	}

	public static string game()
	{
		CppGameConfig cppGameConfig = new CppGameConfig
		{
			world_info = 
			{
				level_id = "114514"
			}
		};
		Term.Write("输入ip：");
		cppGameConfig.room_info.ip = Term.ReadLine();
		Term.Write("输入端口：");
		cppGameConfig.room_info.port = int.Parse(Term.ReadLine());
		cppGameConfig.room_info.token = CppGameM.GetLoginToken();
		cppGameConfig.player_info.user_id = uint.Parse(Http.UID);
		cppGameConfig.player_info.user_name = GetName(app.player_info);
		cppGameConfig.player_info.urs = Http.UID;
		cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
		cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
		cppGameConfig.skin_info.skin_iid = "-1";
		cppGameConfig.misc.multiplayer_game_type = 1000;
		cppGameConfig.misc.auth_server_url = "https://g79authobt.nie.netease.com";
		cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
		cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
		cppGameConfig.world_info.name = "114514";
		cppGameConfig.room_info.item_ids = app.Temp.item_ids;
		string text = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
		if (app.Temp.cppconfig_encryption)
		{
			text = text.Encrypt();
		}
		return text;
	}

	public static string AUTHgame()
	{
		CppGameConfig cppGameConfig = new CppGameConfig
		{
			world_info = 
			{
				level_id = "114514"
			}
		};
		Term.Write("输入multiplayer_game_type：");
		cppGameConfig.misc.multiplayer_game_type = int.Parse(Term.ReadLine());
		Term.Write("输入ip：");
		cppGameConfig.room_info.ip = Term.ReadLine();
		Term.Write("输入端口：");
		cppGameConfig.room_info.port = int.Parse(Term.ReadLine());
		cppGameConfig.room_info.token = CppGameM.GetLoginToken();
		cppGameConfig.player_info.user_id = uint.Parse(Http.UID);
		cppGameConfig.player_info.user_name = GetName(app.player_info);
		cppGameConfig.player_info.urs = Http.UID;
		cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
		cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
		cppGameConfig.skin_info.skin_iid = "-1";
		cppGameConfig.misc.auth_server_url = Http.AuthServerUrl;
		cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
		cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
		cppGameConfig.world_info.name = "114514";
		cppGameConfig.room_info.item_ids = app.Temp.item_ids;
		string text = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
		if (app.Temp.cppconfig_encryption)
		{
			text = text.Encrypt();
		}
		return text;
	}

	public static string FastAUTHgame()
	{
		CppGameConfig cppGameConfig = new CppGameConfig();
		cppGameConfig.world_info.level_id = "114514";
		cppGameConfig.misc.multiplayer_game_type = 100;
		cppGameConfig.room_info.ip = "play.bjdmc.net";
		cppGameConfig.room_info.port = 19132;
		cppGameConfig.room_info.token = CppGameM.GetLoginToken();
		cppGameConfig.player_info.user_id = uint.Parse(Http.UID);
		cppGameConfig.player_info.user_name = GetName(app.player_info);
		cppGameConfig.player_info.urs = Http.UID;
		cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
		cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
		cppGameConfig.skin_info.skin_iid = "-1";
		cppGameConfig.misc.auth_server_url = Http.AuthServerUrl;
		cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
		cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
		cppGameConfig.world_info.name = "114514";
		cppGameConfig.room_info.item_ids = app.Temp.item_ids;
		string text = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
		if (app.Temp.cppconfig_encryption)
		{
			text = text.Encrypt();
		}
		return text;
	}

	public static string FastAUTHec()
	{
		CppGameConfig cppGameConfig = new CppGameConfig();
		cppGameConfig.world_info.level_id = "114514";
		cppGameConfig.misc.multiplayer_game_type = 100;
		cppGameConfig.room_info.ip = "101.245.163.6";
		cppGameConfig.room_info.port = 19113;
		cppGameConfig.room_info.token = CppGameM.GetLoginToken();
		cppGameConfig.player_info.user_id = uint.Parse(Http.UID);
		cppGameConfig.player_info.user_name = GetName(app.player_info);
		cppGameConfig.player_info.urs = Http.UID;
		cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
		cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
		cppGameConfig.skin_info.skin_iid = "-1";
		cppGameConfig.misc.auth_server_url = Http.AuthServerUrl;
		cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
		cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
		cppGameConfig.world_info.name = "114514";
		cppGameConfig.room_info.item_ids = app.Temp.item_ids;
		string text = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
		if (app.Temp.cppconfig_encryption)
		{
			text = text.Encrypt();
		}
		return text;
	}

	public static string game_(string ip, int port, int multiplayer_game_type)
	{
		CppGameConfig cppGameConfig = new CppGameConfig();
		cppGameConfig.world_info.level_id = "114514";
		cppGameConfig.room_info.ip = ip;
		cppGameConfig.room_info.port = port;
		cppGameConfig.room_info.token = CppGameM.GetLoginToken();
		cppGameConfig.player_info.user_id = uint.Parse(Http.UID);
		cppGameConfig.player_info.user_name = GetName(app.player_info);
		cppGameConfig.player_info.urs = Http.UID;
		cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
		cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
		cppGameConfig.skin_info.skin_iid = "-1";
		cppGameConfig.misc.multiplayer_game_type = multiplayer_game_type;
		cppGameConfig.misc.auth_server_url = "https://g79authobt.nie.netease.com";
		cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
		cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
		cppGameConfig.world_info.name = "114514";
		cppGameConfig.room_info.item_ids = app.Temp.item_ids;
		string text = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
		if (app.Temp.cppconfig_encryption)
		{
			text = text.Encrypt();
		}
		return text;
	}

	public static string LanGame()
	{
		CppGameConfig cppGameConfig = new CppGameConfig
		{
			world_info = 
			{
				level_id = "114514"
			}
		};
		Term.Write("输入ip：");
		cppGameConfig.room_info.ip = Term.ReadLine();
		Term.Write("输入端口：");
		cppGameConfig.room_info.port = int.Parse(Term.ReadLine());
		cppGameConfig.room_info.token = CppGameM.GetLoginToken();
		cppGameConfig.player_info.user_id = uint.Parse(Http.PEUID);
		cppGameConfig.player_info.user_name = GetName(app.player_info);
		cppGameConfig.player_info.urs = Http.UID;
		cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
		cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
		cppGameConfig.skin_info.skin_iid = "-1";
		cppGameConfig.misc.multiplayer_game_type = 1;
		cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
		cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
		cppGameConfig.world_info.name = "114514";
		cppGameConfig.room_info.item_ids = app.Temp.item_ids;
		string text = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
		if (app.Temp.cppconfig_encryption)
		{
			text = text.Encrypt();
		}
		return text;
	}

	public static string String2Unicode(string source)
	{
		byte[] bytes = Encoding.Unicode.GetBytes(source);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < bytes.Length; i += 2)
		{
			stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
		}
		return stringBuilder.ToString();
	}

	public static string Unicode2String(string source)
	{
		return new Regex("\\\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(source, (Match x) => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
	}

	public static string GetTimeStamp()
	{
		return Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds).ToString();
	}

	public static ulong GetTimeStampUlong()
	{
		return Convert.ToUInt64((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds);
	}

	public static async void AUpdateLogin()
	{
		try
		{
			await Task.Run(() =>
			{
				while (delete)
				{
					Term.WriteLine(Http.UpdateAuthotp(app.Temp.url));
				}
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Term.WriteLine(ex2.Message);
		}
	}

	public static void UpdateToken()
	{
		try
		{
			byte[] loginTokenBytes = CppGameM.GetLoginTokenBytes();
			byte[] message = SimplePackHelper.SimplePack((ushort)2833, (ushort)loginTokenBytes.Length, loginTokenBytes);
			app.GameRpcPort.SendControlData(message);
		}
		catch
		{
		}
	}

	public static async void GameAsync(string cfg, RPCPort rPCPort)
	{
		await Task.Run(() =>
		{
			try
			{
				File.WriteAllText(app.Temp.path + "\\mc.cfg", cfg);
				Term.WriteLine("\n游戏启动成功！");
				app.InvokeCmd("start " + app.Temp.path + "\\Minecraft.Windows.exe");
				rPCPort.CloseControlConnection();
			}
			catch
			{
				Term.WriteLine("路径错误，请检查路径是否存在！");
			}
		});
	}

	public static async void GameLeftLobbyAsync(string cfg)
	{
		await Task.Run(() =>
		{
			try
			{
				File.WriteAllText(app.Temp.path + "\\mc.cfg", cfg);
				Term.WriteLine("\n游戏启动成功！");
				app.InvokeCmd("start " + app.Temp.path + "\\Minecraft.Windows.exe");
				Http.postAPIHost("{}", "/leave-main-city", "https://g79mclobt.minecraft.cn");
			}
			catch
			{
				Term.WriteLine("路径错误，请检查路径是否存在！");
			}
		});
	}

	public static int JoinLobbyRoom(string rid)
	{
		if (JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"room_id\":\"" + rid + "\",\"password\":\"\",\"check_visibilily\":true}", "/online-lobby-room-enter"))["code"].ToString() == "0")
		{
			Http.room_id_public = rid;
			return 0;
		}
		return 1;
	}

	public static int UJoinLobbyRoom(string rid)
	{
		if (JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"room_id\":\"" + rid + "\",\"password\":\"\",\"check_visibilily\":true}", "/online-lobby-room-enter"))["code"].ToString() == "0" && !app.isroom)
		{
			Http.room_id_public = rid;
			app.updateroom(rid);
			return 0;
		}
		return 1;
	}

	public static int OutRoomList(LobbyGameRoomEntity entity)
	{
		Term.WriteLine("===============================================\n房间号：" + entity.room_name + " --------- 房间entity_id：" + entity.entity_id + "\nitem_id：" + entity.res_id + "\n房间人数：" + entity.cur_num + "/" + entity.max_count + "              " + GetOwnerName(entity.owner_id) + "       " + GetItemName(entity.res_id));
		return 0;
	}

	public static int OutRoomListPE(LobbyGameRoomEntityPE entity)
	{
		Term.WriteLine("===============================================\n房间号：" + entity.room_name + " --------- 房间entity_id：" + entity.entity_id + "\nitem_id：" + entity.res_id + "\n房间人数：" + entity.cur_num + "/" + entity.max_count + "              " + GetOwnerName(entity.owner_id));
		return 0;
	}

	public static string GetItemName(string item_id)
	{
		return JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/item/query/search-mcgame-item-list", "{\"item_ids\":[\"" + item_id + "\"],\"entity_ids\":[]}"))["entities"][0].ToString())["name"].ToString();
	}

	public static string GetOwnerName(string UID)
	{
		return JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/user/query/search-by-uid", "{\"user_id\":\"" + UID + "\"}"))["entity"].ToString())["name"].ToString();
	}

	public static bool UnzipMCZIP(string path)
	{
		try
		{
			string[] allMcZip = GetAllMcZip(path);
			string[] array = allMcZip;
			foreach (string text in array)
			{
				Term.WriteLine(text);
				int num = sd.k(text);
				if (num != 0)
				{
					Term.WriteLine("Decrypt Failed! code:" + num);
					break;
				}
				DeleteFile(text);
			}
			return true;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return false;
		}
	}

	public static bool UnzipModJsonMCZIP(string path, string key, string uuid)
	{
		try
		{
			string[] allMcZip = GetAllMcZip(path);
			string[] array = allMcZip;
			foreach (string text in array)
			{
				Term.WriteLine(text);
				int num = UnzipModJson(key, text, uuid);
				if (num != 0)
				{
					Term.WriteLine("Decrypt Failed! code:" + num);
					break;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return false;
		}
	}

	public static bool UnzipModJsonPath(string path, string key, string uuid)
	{
		try
		{
			string[] allMcZip = GetAllMcZip(path);
			string[] allFiless = GetAllFiless(path);
			string[] allFiles = GetAllFiles(path, ".mergedmcs");
			string[] array = allFiless;
			string[] array2 = array;
			foreach (string text in array2)
			{
				Term.WriteLine(text);
				int num = UnUnzipJson(key, text, uuid);
				if (num != 0)
				{
					Term.WriteLine("Decrypt Failed! code:" + num);
					break;
				}
			}
			array = allFiles;
			string[] array3 = array;
			foreach (string text2 in array3)
			{
				Term.WriteLine(text2);
				ZipFile.ExtractToDirectory(text2, Path.GetDirectoryName(text2.Substring(0, text2.Length - 10)));
				DeleteFile(text2);
			}
			array = allMcZip;
			string[] array4 = array;
			foreach (string text3 in array4)
			{
				Term.WriteLine(text3);
				ZipFile.ExtractToDirectory(text3, Path.GetDirectoryName(text3.Substring(0, text3.Length - 6)));
				DeleteFile(text3);
			}
			return true;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return false;
		}
	}

	public static bool UnUnzipJsonPath(string path, string key, string uuid)
	{
		try
		{
			string[] allFiless = GetAllFiless(path);
			string[] allFiles = GetAllFiles(path, ".mergedmcs");
			string[] array = allFiless;
			string[] array2 = array;
			foreach (string text in array2)
			{
				Term.WriteLine(text);
				int num = UnUnzipJson(key, text, uuid);
				if (num != 0)
				{
					Term.WriteLine("Decrypt Failed! code:" + num);
					break;
				}
			}
			array = allFiles;
			string[] array3 = array;
			foreach (string text2 in array3)
			{
				Term.WriteLine(text2);
				ZipFile.ExtractToDirectory(text2, Path.GetDirectoryName(text2.Substring(0, text2.Length - 10)));
			}
			return true;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return false;
		}
	}

	public static int UnUnzipJson(string key, string path, string uuid)
	{
		try
		{
			byte[] array = x19Crypt.DecryptModJson(FileContent(path), Encoding.ASCII.GetBytes(key), uuid);
			if (array != null)
			{
				Term.WriteLine(path);
				DeleteFile(path);
				FileHelper.ByteToFile(array, path);
				return 0;
			}
			return 0;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return 1;
		}
	}

	public static int UnzipModJson(string key, string path, string uuid)
	{
		try
		{
			byte[] array = x19Crypt.DecryptModJson(FileContent(path), Encoding.ASCII.GetBytes(key), uuid);
			if (array == null)
			{
				ZipFile.ExtractToDirectory(path, Path.GetDirectoryName(path.Substring(0, path.Length - 6)));
				DeleteFile(path);
				return 0;
			}
			DeleteFile(path);
			FileHelper.ByteToFile(array, path);
			ZipFile.ExtractToDirectory(path, Path.GetDirectoryName(path.Substring(0, path.Length - 6)));
			DeleteFile(path);
			return 0;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return 1;
		}
	}

	public static string[] GetAllMcZip(string path)
	{
		return Directory.GetFiles(path, "*.mczip", SearchOption.AllDirectories);
	}

	public static string[] GetAllpng(string path)
	{
		return Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
	}

	public static string[] GetAlljpeg(string path)
	{
		return Directory.GetFiles(path, "*.jpeg", SearchOption.AllDirectories);
	}

	public static string[] GetAlljpg(string path)
	{
		return Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
	}

	public static string[] GetAlljson(string path)
	{
		return Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
	}

	public static string[] GetAlllang(string path)
	{
		return Directory.GetFiles(path, "*.lang", SearchOption.AllDirectories);
	}

	public static string[] GetAllmaterial(string path)
	{
		return Directory.GetFiles(path, "*.material", SearchOption.AllDirectories);
	}

	public static string[] GetAlltga(string path)
	{
		return Directory.GetFiles(path, "*.tga", SearchOption.AllDirectories);
	}

	public static string[] GetAllFiles(string path, string name)
	{
		return Directory.GetFiles(path, "*" + name, SearchOption.AllDirectories);
	}

	public static string[] GetAllFiless(string path)
	{
		List<string> list = new List<string>();
		List<string> list2 = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
		for (int i = 0; i < list2.Count; i++)
		{
			if (Directory.Exists(list2[i]))
			{
				list.Concat(GetAllFiless(list2[i]));
			}
			else
			{
				list.Add(list2[i]);
			}
		}
		return list.ToArray();
	}

	public static bool DeleteFile(string fileFullPath)
	{
		if (File.Exists(fileFullPath))
		{
			if (File.GetAttributes(fileFullPath) == FileAttributes.Directory)
			{
				Directory.Delete(fileFullPath, recursive: true);
				return true;
			}
			File.Delete(fileFullPath);
			return true;
		}
		return false;
	}

	public static string GetDecryptionKey(string device_id, string TextContentKey, string user_id)
	{
		try
		{
			string text = "TG8hVJD3Lt1r86Cv" + user_id + device_id;
			int length = text.Length;
			int num = 0;
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			byte[] array = Convert.FromBase64String(TextContentKey);
			for (int num2 = length - 1; num2 != -1; num2--)
			{
				num = num2 % 16;
				array[num] ^= bytes[num2];
			}
			return Encoding.ASCII.GetString(array);
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	private static byte[] FileContent(string fileName)
	{
		using FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		try
		{
			byte[] array = new byte[fileStream.Length];
			fileStream.Read(array, 0, (int)fileStream.Length);
			return array;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	private static void GetAllDirList(string strBaseDir)
	{
		DirectoryInfo[] directories = new DirectoryInfo(strBaseDir).GetDirectories();
		for (int i = 0; i < directories.Length; i++)
		{
			al.Add(directories[i].FullName);
			GetAllDirList(directories[i].FullName);
		}
	}

	public static string ReplaceString(string JsonString)
	{
		if (JsonString == null)
		{
			return JsonString;
		}
		if (JsonString.Contains("\\"))
		{
			JsonString = JsonString.Replace("\\", "\\\\");
		}
		if (JsonString.Contains("'"))
		{
			JsonString = JsonString.Replace("'", "\\'");
		}
		if (JsonString.Contains("\""))
		{
			JsonString = JsonString.Replace("\"", "\\\"");
		}
		JsonString = Regex.Replace(JsonString, "[\\n\\r]", "");
		JsonString = JsonString.Trim();
		return JsonString;
	}

	public static void TraceUser(string uid)
	{
		new Thread(() =>
		{
			app.start_stop = true;
			_ = string.Empty;
			string text = string.Empty;
			string text2 = string.Empty;
			string text3 = "0";
			bool flag = false;
			while (app.start_stop)
			{
				string playerDisplayName = app.GetPlayerDisplayName(uid);
				string value = Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/search-by-name-v2", "{\"keyword\":\"" + playerDisplayName + "\",\"length\":10,\"offset\":0,\"version\":\"" + app.Temp.Version + "\"}");
				try
				{
					JObject jObject = JsonConvert.DeserializeObject<JObject>(value);
					text = jObject["entities"][0]["entity_id"].ToString();
					text2 = jObject["entities"][0]["res_id"].ToString();
				}
				catch
				{
				}
				if (!flag || text3 != text)
				{
					app.InvokeCmd("taskkill /f /im \"LobbyRoomDummy.exe\"");
					app.InvokeCmdAsync("start LobbyRoomDummy.exe --roomentityid " + text + " --leftid " + text3 + " --purchase " + text2);
				}
				flag = true;
				text3 = text;
				Thread.Sleep(2000);
			}
		}).Start();
	}

	public static void TraceUserFB(string uid)
	{
		new Thread(() =>
		{
			app.start_stop = true;
			_ = string.Empty;
			string text = string.Empty;
			string text2 = string.Empty;
			string text3 = "0";
			bool flag = false;
			while (app.start_stop)
			{
				string playerDisplayName = app.GetPlayerDisplayName(uid);
				string value = Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/search-by-name-v2", "{\"keyword\":\"" + playerDisplayName + "\",\"length\":10,\"offset\":0,\"version\":\"" + app.Temp.Version + "\"}");
				try
				{
					JObject jObject = JsonConvert.DeserializeObject<JObject>(value);
					text = jObject["entities"][0]["entity_id"].ToString();
					text2 = jObject["entities"][0]["res_id"].ToString();
				}
				catch
				{
				}
				if (!flag || text3 != text)
				{
					app.InvokeCmd("taskkill /f /im \"LobbyRoomDummy.exe\"");
					app.InvokeCmdAsync("start LobbyRoomDummy.exe --roomentityid " + text + " --leftid " + text3 + " --purchase " + text2);
				}
				flag = true;
				text3 = text;
				Thread.Sleep(2000);
			}
		}).Start();
	}

	public static string StringToUnicode(string str)
	{
		char[] array = str.ToCharArray();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.AppendFormat("\\u{0:x4}", (int)array[i]);
		}
		return stringBuilder.ToString();
	}

	public static string Base64Encode(byte[] plainText)
	{
		return Convert.ToBase64String(plainText);
	}

	public static string GetRandomKey(int length)
	{
		byte[] array = new byte[4];
		new RNGCryptoServiceProvider().GetBytes(array);
		Random random = new Random(BitConverter.ToInt32(array, 0));
		string text = null;
		string text2 = "0123456789abcdef";
		for (int i = 0; i < length; i++)
		{
			text += text2.Substring(random.Next(0, text2.Length - 1), 1);
		}
		return text;
	}

	public static string GetRandomKeyv2(int length)
	{
		byte[] array = new byte[4];
		new RNGCryptoServiceProvider().GetBytes(array);
		Random random = new Random(BitConverter.ToInt32(array, 0));
		string text = null;
		string text2 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		for (int i = 0; i < length; i++)
		{
			text += text2.Substring(random.Next(0, text2.Length - 1), 1);
		}
		return text;
	}

	public static string GetVersion()
	{
		return JsonConvert.DeserializeObject<JObject>(Http.getAPIPC("/online-lobby-status", output: false))["entity"]["client_mc_version"].ToString();
	}

	public static void SaveFile(string filename, string data)
	{
		StreamWriter streamWriter = new StreamWriter(new FileStream(filename, FileMode.Create));
		streamWriter.WriteLine(data);
		streamWriter.Close();
	}

	public static string GetCommonName(string UID, string token)
	{
		string value = x19Crypt.ComputeDynamicToken("/user-detail", "", token);
		HttpContent httpContent = new StringContent("");
		httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		httpContent.Headers.Add("user-token", value);
		httpContent.Headers.Add("user-id", UID);
		string result = Http.client.PostAsync("https://x19apigatewayobt.nie.netease.com/user-detail", httpContent).Result.Content.ReadAsStringAsync().Result;
		if (GetName(result) == "")
		{
			string randomKeyv;
			do
			{
				randomKeyv = GetRandomKeyv2(8);
			}
			while (!(JsonConvert.DeserializeObject<JObject>(Http.postAPIPC_NoCommon("{\"name\":\"" + randomKeyv + "\",\"entity_id\":\"" + UID + "\"}", "/nickname-setting", token, UID))["code"].ToString() == "0"));
			return randomKeyv;
		}
		return GetName(result);
	}
}
