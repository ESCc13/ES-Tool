using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppLogin.NetEase;
using FastBuilder.Utils;
using Login;
using Login.JavaHelper;
using Login.JavaHelper.JavaConfigHelper;
using Login.Native;
using Login.NetEase;
using Login.NetEase.ChatService;
using Login.NetEase.DummyService;
using Login.NetEase.DummyService.Entity;
using Login.NetEase.Entity;
using Login.NetEase.LinkService;
using Login.NetEase.LoginotpList;
using Login.NetEase.RPCServer;
using Login.NetEase.Request;
using Login.NetEase.Utils;
using Login.loginauth;
using MCStudio.Model.EnumType;
using MCStudio.Network;
using MCStudio.Utils;
using Mark;
using Mcl.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noya.LocalServer.Common.Extensions;
using WPFLauncher.Manager.Configuration.CppConfigure;
using WPFLauncher.Network.Launcher;
using WPFLauncher.Util;
using text.loginauth;

namespace text;

internal class app
{
	public static ChatConnection chatConnection;

	public static LinkConnection linkConnection;

	public static temp Temp { get; set; }

	public static RPCPort GameRpcPort { get; set; }

	public static string PostHost { get; set; } = "https://x19apigatewayobt.nie.netease.com";

	public static string player_info { get; set; }

	private static string Login_otp { get; set; }

	public static bool isroom { get; set; }

	public static bool start_stop { get; set; }

	public static bool kick_stop { get; set; }

	private static string SavePermissions { get; set; } = "false";

	private static int JoinPermissions { get; set; } = 0;

	private static bool sajoin { get; set; } = true;

	public static bool PE_Login { get; set; } = false;

	public static string displayName { get; set; }

	public static void AppRun()
	{
		do
		{
			Login_otp = LoginInfo.CookleChoose();
		}
		while (Login_otp == null);
		Term.Clear();
		if (Login_otp == "o")
		{
			Command();
			return;
		}
		if (Login_otp == null)
		{
			Term.WriteLine("登录失败");
		}
		Term.WriteLine("正在登录中，请稍等...");
		if (Temp.is_g79 || PE_Login)
		{
			Http.PE_Login(Login_otp);
		}
		else
		{
			Http.Login(Login_otp);
		}
		PE_Login = false;
		if (Temp.chat_connect || ChatConnection.Is_Connected)
		{
			chatConnection = new ChatConnection();
		}
		if (Temp.InitializeName)
		{
			Name();
		}
		Function.GetName(player_info);
		Term.WriteLine("你好！冒险家：" + displayName);
		Term.WriteLine("输入help可查看指令");
		Command();
	}

	public static void AppLoad(string Login_otp)
	{
		Term.WriteLine("正在登录中，请稍等...");
		Http.Login(Login_otp);
		if (Temp.InitializeName)
		{
			Name();
		}
		Function.GetName(player_info);
		Term.WriteLine("你好！冒险家：" + displayName);
		Term.WriteLine("输入help可查看指令");
		Command();
	}

	public static void AppRun_(string response)
	{
		try
		{
			Term.WriteLine("正在登录中，请稍等...");
			Http.LoadToken(ReadJson(response));
			if (Temp.InitializeName)
			{
				Name();
			}
			Term.WriteLine("你好！冒险家：" + Function.GetName(player_info));
			Term.WriteLine("输入help可查看指令");
			Command();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
		}
	}

	public static void ui()
	{
		Term.WriteLine("=============================");
		Term.WriteLine("获取CPPtoken ---- geth5token");
		Term.WriteLine("更新token ------ updatetoken");
		Term.WriteLine("重新登录 -------------- anew");
		Term.WriteLine("起名 --------------- setname");
		Term.WriteLine("获得名字 ----------- getname");
		Term.WriteLine("退出登录 ------------ delete");
		Term.WriteLine("改名 ------------ nameupdate");
		Term.WriteLine("设置游戏路径 ------- setpath");
		Term.WriteLine("获取游戏 ----------- getgame");
		Term.WriteLine("启动游戏 ------------- start");
		Term.WriteLine("联机大厅开房间 ------ startr");
		Term.WriteLine("允许房客保存 --- visitorsave");
		Term.WriteLine("保存地图 -------------- save");
		Term.WriteLine("载入地图 -------------- load");
		Term.WriteLine("踢人 ------------------ kick");
		Term.WriteLine("房间可见性 ------ visibility");
		Term.WriteLine("转交房主 ------------- setop");
		Term.WriteLine("购买组件 ---------- purchase");
		Term.WriteLine("=============================");
	}

	/// <summary>
	/// 启动类命令的统一前置检查。
	/// 原来这些命令各自写成「路径为空打一行、itemid 为空再打一行」的并列 if，条件不满足时
	/// 只是多打一行日志就继续往下走（甚至直接跳过），而 GUI 那边已经把整串参数投进了输入队列，
	/// 没人消费就会串到后续命令上 —— 用户看到的现象就是「日志一切正常，游戏没启动」。
	/// 这里一次性把「能不能启动」判断清楚，不满足就把排队参数丢掉并返回 false。
	/// </summary>
	/// <param name="pendingArgs">GUI 已经排进队列、需要丢弃的参数行数</param>
	private static bool CheckStart(int pendingArgs)
	{
		string why = null;
		if (Temp == null)
		{
			why = "配置没加载成功（config.json 读不出来）。";
		}
		else if (string.IsNullOrEmpty(Temp.path))
		{
			why = "还没有设置游戏路径。";
		}
		else if (Temp.item_ids == null || Temp.item_ids.Count == 0)
		{
			why = "config.json 里的 item_ids 是空的，网易接口取不到资源 id，客户端进不了游戏。";
		}
		if (why == null)
		{
			return true;
		}
		if (pendingArgs > 0)
		{
			int dropped = Term.Discard(pendingArgs, 300);
			if (dropped < pendingArgs)
			{
				Term.WriteLine("（提示：还有 " + (pendingArgs - dropped) + " 行参数没取到，若后续命令错位请重新点一次）");
			}
		}
		Term.WriteLine("无法启动：" + why);
		if (why.Contains("item_ids"))
		{
			Term.WriteLine("请到「设置 → 编辑 config.json → Item ID 列表」填上资源 id（多个用逗号分隔），保存后即可生效。");
		}
		else
		{
			Term.WriteLine("请到「设置」页填好游戏路径并保存。");
		}
		return false;
	}

	/// <summary>重新把 config.json 读进 Temp（GUI 改完配置后立即生效，不必重启启动器）。</summary>
	public static void ReloadConfig()
	{
		Temp = JsonConvert.DeserializeObject<temp>(ReadJson("config.json"));
	}

	public static void Help()
	{
		Term.WriteLine("显示该列表 --------------------------- help");
		Term.WriteLine("查看版本号 --------------------------- version");
		Term.WriteLine("获取CPPtoken ------------------------- geth5token");
		Term.WriteLine("获取token ---------------------------- gettoken");
		Term.WriteLine("获取DToken --------------------------- getdtoken");
		Term.WriteLine("更新token ---------------------------- updatetoken");
		Term.WriteLine("重新登录 ----------------------------- anew");
		Term.WriteLine("起名 --------------------------------- setname");
		Term.WriteLine("获得名字 ----------------------------- getname");
		Term.WriteLine("退出登录 ----------------------------- delete");
		Term.WriteLine("改名 --------------------------------- nameupdate");
		Term.WriteLine("设置游戏路径 ------------------------- setpath");
		Term.WriteLine("获取游戏 ----------------------------- getgame");
		Term.WriteLine("启动游戏：自定义ip端口 --------------- ipstart");
		Term.WriteLine("启动游戏 ----------------------------- start");
		Term.WriteLine("启动游戏（启动器端口） --------------- portstart");
		Term.WriteLine("启动游戏设置模式---------------------- gametype");
		Term.WriteLine("认证启动游戏 ------------------------- authstart");
		Term.WriteLine("联机大厅开房间 ----------------------- startr");
		Term.WriteLine("手机版联机大厅开房间 ----------------- startrPE");
		Term.WriteLine("允许房客保存 ------------------------- visitorsave");
		Term.WriteLine("保存地图 ----------------------------- save");
		Term.WriteLine("重置地图 ----------------------------- reset");
		Term.WriteLine("载入地图 ----------------------------- load");
		Term.WriteLine("载入手机版地图 ----------------------- loadPE");
		Term.WriteLine("踢人 --------------------------------- kick");
		Term.WriteLine("一键踢所有人 ------------------------- kickall");
		Term.WriteLine("房间密码热更新 ----------------------- setpassword");
		Term.WriteLine("房间可见性 --------------------------- visibility");
		Term.WriteLine("转交房主 ----------------------------- setop");
		Term.WriteLine("购买组件 ----------------------------- purchase");
		Term.WriteLine("购买手机版组件 ----------------------- purchasePE");
		Term.WriteLine("启动游戏添加Itemid ------------------- additem");
		Term.WriteLine("删除Itemid --------------------------- ritem");
		Term.WriteLine("搜索房间号或联机大厅组件 ------------- search");
		Term.WriteLine("搜索手机版房间号或联机大厅组件 ------- searchPE");
		Term.WriteLine("通过房主名称搜索房间 ----------------- searchroom");
		Term.WriteLine("通过房主名称搜索房间 ----------------- searchroomv2");
		Term.WriteLine("通过uid查找用户 ---------------------- searchuid");
		Term.WriteLine("通过用户名查找uid -------------------- searchuser");
		Term.WriteLine("进入房间 ----------------------------- join");
		Term.WriteLine("进入房间（不掉） --------------------- ujoin");
		Term.WriteLine("进密码房 ----------------------------- passwordjoin");
		Term.WriteLine("进入手机版房间 ----------------------- joinPE");
		Term.WriteLine("退出房间 ----------------------------- left");
		Term.WriteLine("指定房间id退出房间 ------------------- wleft");
		Term.WriteLine("获取房间ip和端口 --------------------- getip");
		Term.WriteLine("搜索物品id --------------------------- searchitem");
		Term.WriteLine("开发功能：自定义接口调用 ------------- post");
		Term.WriteLine("开发功能：自定义接口调用 ------------- postfile");
		Term.WriteLine("开发功能：自定义接口调用（加密） ----- Epost");
		Term.WriteLine("开发功能：自定义接口调用（加密） ----- Epostfile");
		Term.WriteLine("修改post服务器 ----------------------- setposthost");
		Term.WriteLine("自定义接口调用（修改用户信息） ------- spost");
		Term.WriteLine("自定义接口调用（修改用户信息） ------- spostfile");
		Term.WriteLine("测试认证服务器 ----------------------- auth");
		Term.WriteLine("测试认证服务器v2 --------------------- authv2");
		Term.WriteLine("测试认证服务器 ----------------------- peauth");
		Term.WriteLine("测试认证服务器v2 --------------------- peauthv2");
		Term.WriteLine("测试认证服务器 ----------------------- authpath");
		Term.WriteLine("测试认证服务器v2 --------------------- authpathv2");
		Term.WriteLine("测试认证服务器 ----------------------- peauthpath");
		Term.WriteLine("测试认证服务器v2 --------------------- peauthpathv2");
		Term.WriteLine("循环进入房间 ------------------------- ajoin");
		Term.WriteLine("使用entityid循环进房间 --------------- ajoinrid");
		Term.WriteLine("停止进入房间 ------------------------- STOP");
		Term.WriteLine("获取RTX版 ---------------------------- getrtxgame");
		Term.WriteLine("获取校验地址 ------------------------- getallgame");
		Term.WriteLine("获取64位版 --------------------------- test");
		Term.WriteLine("搜索名字 ----------------------------- searchuser");
		Term.WriteLine("封禁玩家 ----------------------------- ban");
		Term.WriteLine("启动封禁 ----------------------------- startban");
		Term.WriteLine("清除所有ban -------------------------- clearbanid");
		Term.WriteLine("解ban -------------------------------- unban");
		Term.WriteLine("自定义进入游戏 ----------------------- gametype");
		Term.WriteLine("进入手机版多人主城 ------------------- joinlobby");
		Term.WriteLine("开启端口连接启动游戏 ----------------- portstart");
		Term.WriteLine("向游戏发送数据 ----------------------- senddata");
		Term.WriteLine("启动控制连接 ------------------------- startconnect");
		Term.WriteLine("关闭控制连接 ------------------------- closeconnect");
		Term.WriteLine("开启房间自动更新 --------------------- startroomupdate");
		Term.WriteLine("关闭房间自动更新 --------------------- closeroomupdate");
		Term.WriteLine("获取uid ------------------------------ getuid");
		Term.WriteLine("获取peuid ---------------------------- getpeuid");
		Term.WriteLine("获取SRCToken ------------------------- gettoken");
		Term.WriteLine("获取DToken --------------------------- getdtoken");
		Term.WriteLine("初始化解包 --------------------------- getiteminfo");
		Term.WriteLine("获取PEuid ---------------------------- getpeuid");
		Term.WriteLine("获取模组下载地址 --------------------- getmod");
		Term.WriteLine("获取模组下载地址 --------------------- getpemod");
		Term.WriteLine("获取模组下载地址 --------------------- searchmod");
		Term.WriteLine("获取模组下载地址 --------------------- searchpemod");
		Term.WriteLine("解包mczip文件 ------------------------ unzip");
		Term.WriteLine("解密模组的所有文件 ------------------- decryptpath");
		Term.WriteLine("获取模组密钥 ------------------------- getitemkey");
		Term.WriteLine("获取手机版模组密钥 ------------------- getpeitemkey");
		Term.WriteLine("获取房间列表 ------------------------- roomlist");
		Term.WriteLine("获取手机版房间列表 ------------------- roomlistPE");
		Term.WriteLine("手机版房间快速匹配 ------------------- fast");
		Term.WriteLine("通过房间id启动自动更新 --------------- wstartroomupdate");
		Term.WriteLine("获取房间成员列表 --------------------- playerlist");
		Term.WriteLine("通过房间id获取房间成员列表 ----------- wplayerlist");
		Term.WriteLine("启动本地authv2认证 ------------------- startserver");
		Term.WriteLine("启动本地FastBuilder认证 -------------- startserverv2");
		Term.WriteLine("设置服务器id（本地认证前置） --------- serverid");
		Term.WriteLine("设置服务器ip（FB认证前置） ----------- serverip");
		Term.WriteLine("本地联机服务器列表 ------------------- allserver");
		Term.WriteLine("本地联机踢人 ------------------------- mkick");
		Term.WriteLine("追踪锁服 ----------------------------- traceUID");
		Term.WriteLine("追踪崩服 ----------------------------- lockUID");
		Term.WriteLine("启动聊天服务器连接 ------------------- startchatconnect");
		Term.WriteLine("启动Link服务器连接 ------------------- startlinkconnect");
		Term.WriteLine("关闭聊天服务器连接 ------------------- closechatconnect");
		Term.WriteLine("关闭Link服务器连接 ------------------- closelinkconnect");
		Term.WriteLine("自定义link消息 ----------------------- sendlink");
		Term.WriteLine("自定义link消息 ----------------------- sendlinkfiel");
		Term.WriteLine("sign签名校验 ------------------------- getsign");
		Term.WriteLine("生成用户索引文件 --------------------- userlist");
		Term.WriteLine("发送好友消息 ------------------------- senduid");
		Term.WriteLine("发送好友消息 ------------------------- senduidpath");
		Term.WriteLine("远程卡人 ----------------------------- asenduid");
		Term.WriteLine("远程卡人 ----------------------------- asenduidpath");
		Term.WriteLine("加好友 ------------------------------- addfriend");
		Term.WriteLine("远程卡人（随机小号） ----------------- kickplayer");
		Term.WriteLine("一键布吉岛 --------------------------- fastbjd");
		Term.WriteLine("一键EC ------------------------------ fastec");
		Term.WriteLine("搜索pe租赁服 ------------------------- searchserver");
		Term.WriteLine("获取租赁服ip ------------------------- getserverip");
		Term.WriteLine("锁服（随机小号） --------------------- lockserver");
		Term.WriteLine("（FastBuilder）联大崩服 -------------- lockserverv2");
		Term.WriteLine("（FastBuilder）租赁服锁服 ------------ lockserverv3");
		Term.WriteLine("（FastBuilder）联机大厅刷屏 ---------- lockserverv4");
		Term.WriteLine("（FastBuilder）联机大厅字符画 -------- lockserverv5");
		Term.WriteLine("（FastBuilder）租赁服锁服 ------------ lockserverv6");
		Term.WriteLine("（FastBuilder）设置认证服 ------------ setauthserver");
		Term.WriteLine("（FastBuilder）设置认证服v2 ---------- setwebserver");
		Term.WriteLine("（FastBuilder）显示窗口 -------------- displaywindow");
		Term.WriteLine("（FastBuilder）显示实体 -------------- displayentity");
		Term.WriteLine("（FastBuilder）联大循环刷屏 ---------- sproom");
		Term.WriteLine("（FastBuilder）联大循环刷屏 ---------- spserver");
		Term.WriteLine("（ChatBot）刷卡密 -------------------- getkey");
		Term.WriteLine("（ChatBot）初始化 -------------------- initdummy");
		Term.WriteLine("租赁服人机填充 ----------------------- padding");
		Term.WriteLine("设置租赁服填充预留空位 --------------- setnullpadding");
		Term.WriteLine("联大人机填充（锁服） ----------------- paddinglobby");
		Term.WriteLine("设置标题 ----------------------------- title");
		Term.WriteLine("设置颜色 ----------------------------- color");
		Term.WriteLine("清屏 --------------------------------- cls");
		Term.WriteLine("清屏 --------------------------------- clear");
		Term.WriteLine("点号 --------------------------------- report");
	}

	public static void Command()
	{
		while (true)
		{
			Term.Write(Http.UID + ">> ");
			string text = Term.ReadLine();
			if (text == "version")
			{
				Term.WriteLine("#####################################################");
				Term.WriteLine("版本号：1.4.0");
				Term.WriteLine("官方交流群：917598137");
				Term.WriteLine("本工具完全免费，如果你是付费购买本工具说明你被圈了！");
				Term.WriteLine("仅供技术讨论，请勿用于非法用途！");
				Term.WriteLine("#####################################################");
			}
			if (text == "help")
			{
				Help();
			}
			if (text == "geth5token")
			{
				Term.WriteLine(CppGameM.GetLoginToken());
			}
			if (text == "updatetoken")
			{
				try
				{
					Http.LoginDToken = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.UpdateAuthotp(Temp.url))["entity"].ToString())["token"].ToString();
					Http.LoginSRCToken = MclNetClient.GetDecryptToken(Http.LoginDToken);
					Term.WriteLine("token:" + Http.LoginSRCToken);
					Function.UpdateToken();
				}
				catch
				{
					Term.WriteLine("网络断开连接");
				}
			}
			if (text == "delete")
			{
				Http.Close();
				Http.istrue = false;
				isroom = false;
				if (chatConnection != null)
				{
					chatConnection.Close();
				}
				if (linkConnection != null)
				{
					linkConnection.Close();
				}
			}
			if (text == "anew")
			{
				do
				{
					Login_otp = LoginInfo.CookleChoose();
				}
				while (Login_otp == null);
				if (Login_otp == "o")
				{
					Command();
				}
				else
				{
					if (chatConnection != null)
					{
						chatConnection.Close();
					}
					if (linkConnection != null)
					{
						linkConnection.Close();
					}
					Http.Close();
					Term.Clear();
					Term.WriteLine("正在登录中，请稍等...");
					if (Temp.is_g79 || PE_Login)
					{
						Http.PE_Login(Login_otp);
					}
					else
					{
						Http.Login(Login_otp);
					}
					PE_Login = false;
					if (Temp.InitializeName)
					{
						Name();
					}
					Term.WriteLine("你好！冒险家：" + Function.GetName(player_info));
				}
			}
			if (text == "getname")
			{
				Http.GetName();
				Term.WriteLine(Function.GetName(player_info));
			}
			if (text == "setname")
			{
				Term.Write("输入你要设置的昵称：");
				Http.SetName(Term.ReadLine());
				Http.GetName();
				Function.GetName(player_info);
			}
			if (text == "ipstart")
			{
				if (CheckStart(0))
				{
					try
					{
						string contents = Function.game();
						File.WriteAllText(Temp.path + "\\mc.cfg", contents);
						InvokeCmdAsync("start " + Temp.path + "\\Minecraft.Windows.exe");
					}
					catch
					{
						Term.WriteLine("路径错误，请检查路径是否存在！");
					}
				}
			}
			if (text == "authstart")
			{
				if (CheckStart(0))
				{
					if (Http.AuthServerUrl == null)
					{
						Term.WriteLine("你还没有启动认证服务器！");
					}
					else if (Http.server_id == null)
					{
						Term.WriteLine("你还没有设置服务器id！");
					}
					else
					{
						try
						{
							string contents2 = Function.AUTHgame();
							File.WriteAllText(Temp.path + "\\mc.cfg", contents2);
							InvokeCmdAsync("start " + Temp.path + "\\Minecraft.Windows.exe");
						}
						catch
						{
							Term.WriteLine("路径错误，请检查路径是否存在！");
						}
					}
				}
			}
			if (text == "start")
			{
				if (CheckStart(0))
				{
					try
					{
						JObject jObject = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("", "/online-lobby-game-enter"));
						CppGameConfig cppGameConfig = new CppGameConfig();
						cppGameConfig.world_info.level_id = "114514";
						cppGameConfig.room_info.ip = jObject["entity"]["server_host"].ToString();
						cppGameConfig.room_info.port = (int)jObject["entity"]["server_port"];
						cppGameConfig.room_info.token = CppGameM.GetLoginToken();
						cppGameConfig.player_info.user_id = uint.Parse(Http.UID);
						cppGameConfig.player_info.user_name = Function.GetName(player_info);
						cppGameConfig.player_info.urs = Http.UID;
						cppGameConfig.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
						cppGameConfig.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
						cppGameConfig.skin_info.skin_iid = "-1";
						cppGameConfig.misc.multiplayer_game_type = 1000;
						cppGameConfig.misc.auth_server_url = "https://g79authobt.nie.netease.com";
						cppGameConfig.web_server_url = "https://x19apigatewayobt.nie.netease.com";
						cppGameConfig.core_server_url = "https://x19obtcore.nie.netease.com:8443";
						cppGameConfig.world_info.name = "114514";
						cppGameConfig.room_info.item_ids = Temp.item_ids;
						string text2 = JsonConvert.SerializeObject(cppGameConfig, Formatting.Indented);
						if (Temp.cppconfig_encryption)
						{
							text2 = text2.Encrypt();
						}
						File.WriteAllText(Temp.path + "\\mc.cfg", text2);
						InvokeCmdAsync("start " + Temp.path + "\\Minecraft.Windows.exe");
					}
					catch
					{
						Term.WriteLine("路径错误，请检查路径是否存在！");
					}
				}
			}
			if (text == "searchroom")
			{
				Term.Write("输入房间信息：");
				string text3 = Term.ReadLine();
				Term.WriteLine(Regex.Unescape(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/search-by-name-v2", "{\"keyword\":\"" + text3 + "\",\"length\":10,\"offset\":0,\"version\":\"" + Temp.Version + "\"}")));
			}
			if (text == "searchroomv2")
			{
				Term.Write("输入房间信息：");
				string text4 = Term.ReadLine();
				Term.WriteLine(Regex.Unescape(Http.x19post("https://x19mclobt.nie.netease.com", "/online-lobby-room/query/search-by-name-v2", "{\"keyword\":\"" + text4 + "\",\"length\":10,\"offset\":0,\"version\":\"" + Temp.Version + "\"}")));
			}
			if (text == "searchPE")
			{
				Term.Write("输入房间信息：");
				Http.SearchPE(Term.ReadLine());
			}
			if (text == "search")
			{
				Term.Write("输入房间信息：");
				Http.Search(Term.ReadLine());
			}
			if (text == "roomlistPE")
			{
				Term.Write("输入物品id：");
				Http.RoomListPE(Term.ReadLine());
			}
			if (text == "roomlist")
			{
				Term.Write("输入物品id：");
				Http.RoomList(Term.ReadLine());
			}
			if (text == "getroomlist")
			{
				Term.Write("输入物品id：");
				string text5 = Term.ReadLine();
				Http.postAPIPC("{\"res_id\":\"" + text5 + "\",\"version\":\"" + Temp.Version + "\",\"with_friend\":true,\"offset\":0,\"length\":10}", "/online-lobby-room/query/list-room-by-res-id");
			}
			if (text == "quick")
			{
				Term.Write("输入物品id：");
				string text6 = Term.ReadLine();
				JsonConvert.DeserializeObject<ListEntity>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-room/query/list-room-by-res-id", "{\"res_id\":\"" + text6 + "\",\"version\":\"" + Temp.Version + "\",\"with_friend\":true,\"offset\":0,\"length\":10}"));
			}
			if (text == "joinPE")
			{
				Term.Write("输入房间id：");
				Http.joined(Term.ReadLine());
			}
			if (text == "join")
			{
				Term.Write("输入房间id：");
				Function.JoinLobbyRoom(Term.ReadLine());
			}
			if (text == "ujoin")
			{
				Term.Write("输入房间id：");
				Function.UJoinLobbyRoom(Term.ReadLine());
			}
			if (text == "ivjoin")
			{
				Term.Write("输入房间id：");
				string text7 = Term.ReadLine();
				Http.postAPIPC("{\"room_id\":\"" + text7 + "\",\"password\":\"\",\"check_visibilily\":false}", "/online-lobby-room-enter");
			}
			if (text == "passwordjoin")
			{
				Term.Write("输入房间id：");
				string text8 = Term.ReadLine();
				Term.Write("输入房间密码：");
				string text9 = Term.ReadLine();
				Http.postAPIPC("{\"room_id\":\"" + text8 + "\",\"password\":\"" + text9 + "\",\"check_visibilily\":false}", "/online-lobby-room-enter");
			}
			if (text == "wstartroomupdate")
			{
				Term.Write("输入房间entity_id：");
				updateroom(Term.ReadLine());
			}
			if (text == "setroomid")
			{
				Term.Write("输入房间entity_id：");
				Http.room_id_public = Term.ReadLine();
			}
			if (text == "startroomupdate")
			{
				updateroom(Http.room_id_public);
			}
			if (text == "wleft")
			{
				Term.WriteLine("PublicRoomID:" + Http.room_id_public);
				Term.Write("输入房间id：");
				Http.left(Term.ReadLine());
				isroom = false;
			}
			if (text == "left")
			{
				Term.WriteLine("PublicRoomID:" + Http.room_id_public);
				Http.left(Http.room_id_public);
				isroom = false;
			}
			if (text == "getip")
			{
				Http.GetIP();
			}
			if (text == "purchasePE")
			{
				Term.Write("输入物品id：");
				Http.PurchaseItem(Term.ReadLine());
			}
			if (text == "purchase")
			{
				Term.Write("输入物品id：");
				Http.PurchaseItemPC(Term.ReadLine());
			}
			if (text == "command")
			{
				Term.Write("输入房间id：");
				string rid = Term.ReadLine();
				Term.Write("输入指令：");
				string command = Term.ReadLine();
				Http.aicommand(rid, command);
			}
			if (text == "nameupdate")
			{
				Term.Write("输入新的名字：");
				Http.NameUpdate(Term.ReadLine());
			}
			if (text == "searchitem")
			{
				Term.Write("输入物品id：");
				Http.SearchItem(Term.ReadLine());
			}
			if (text == "setpath")
			{
				Term.Write("输入windowsmc的路径：");
				Temp.path = Term.ReadLine();
				File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
				Temp = JsonConvert.DeserializeObject<temp>(ReadJson("config.json"));
				Term.WriteLine("添加成功");
			}
			if (text == "additem")
			{
				Term.WriteLine("输入clear重置列表 置顶ID必须有效（4641943113293411081）");
				Term.Write("输入物品id：");
				string text10 = Term.ReadLine();
				if (text10 == "clear")
				{
					Temp.item_ids.Clear();
					File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
					Term.WriteLine("重置成功");
				}
				else if (Temp.item_ids.Contains(text10))
				{
					Term.WriteLine("已添加过：" + text10);
				}
				else
				{
					Temp.item_ids.Add(text10);
					File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
					Term.WriteLine("添加成功");
				}
			}
			if (text == "ritem")
			{
				Term.Write("输入物品id：");
				string item = Term.ReadLine();
				bool flag = Temp.item_ids.Remove(item);
				File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
				if (flag)
				{
					Term.WriteLine("删除成功");
				}
			}
			if (text == "getgame")
			{
				Http.GetGame();
			}
			if (text == "unzip")
			{
				try
				{
					Term.Write("JsonPath:");
					UnzipModJson(Term.ReadLine());
				}
				catch (Exception ex)
				{
					Term.WriteLine(ex.Message);
				}
			}
			if (text == "unzippath")
			{
				Term.Write("Path:");
				Function.UnzipMCZIP(Term.ReadLine());
			}
			if (text == "Epost")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url = Term.ReadLine();
					Term.Write("POST:");
					string requests = Term.ReadLine();
					Http.EncyptPOST(url, requests, PostHost);
				}
				catch (Exception ex2)
				{
					Term.WriteLine(ex2.Message);
				}
			}
			if (text == "Epostfile")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url2 = Term.ReadLine();
					Term.Write("PATH:");
					string filePath = Term.ReadLine();
					Http.EncyptPOST(url2, ReadJson(filePath), PostHost);
				}
				catch (Exception ex3)
				{
					Term.WriteLine(ex3.Message);
				}
			}
			if (text == "testEpost")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url3 = Term.ReadLine();
					Term.Write("POST:");
					string requests2 = Term.ReadLine();
					Term.WriteLine("g79V3: 1");
					Term.WriteLine("x19V2: 2");
					Term.WriteLine("g79VC: 3");
					Term.Write("EncryptType:");
					switch (Term.ReadLine())
					{
					case "1":
						Http.EncyptPOST_g79v3(url3, requests2, PostHost);
						break;
					case "2":
						Http.EncyptPOST(url3, requests2, PostHost);
						break;
					case "3":
						Http.EncyptPOST_g79v12(url3, requests2, PostHost);
						break;
					default:
						Term.WriteLine("Value Error!");
						break;
					}
				}
				catch (Exception ex4)
				{
					Term.WriteLine(ex4.Message);
				}
			}
			if (text == "spost")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url4 = Term.ReadLine();
					Term.Write("POST:");
					string requests3 = Term.ReadLine();
					Term.Write("UID:");
					string uid = Term.ReadLine();
					Term.Write("TOKEN:");
					string token = Term.ReadLine();
					Http.postAPIHost(requests3, url4, PostHost, uid, token);
				}
				catch (Exception ex5)
				{
					Term.WriteLine(ex5.Message);
				}
			}
			if (text == "spostfile")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url5 = Term.ReadLine();
					Term.Write("PATH:");
					string filePath2 = Term.ReadLine();
					Term.Write("UID:");
					string uid2 = Term.ReadLine();
					Term.Write("TOKEN:");
					string token = Term.ReadLine();
					Http.postAPIHost(ReadJson(filePath2), url5, PostHost, uid2, token);
				}
				catch (Exception ex6)
				{
					Term.WriteLine(ex6.Message);
				}
			}
			if (text == "post")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url6 = Term.ReadLine();
					Term.Write("POST:");
					Http.postAPIHost(Term.ReadLine(), url6, PostHost);
				}
				catch (Exception ex7)
				{
					Term.WriteLine(ex7.Message);
				}
			}
			if (text == "postfile")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url7 = Term.ReadLine();
					Term.Write("PATH:");
					Http.postAPIHost(ReadJson(Term.ReadLine()), url7, PostHost);
				}
				catch (Exception ex8)
				{
					Term.WriteLine(ex8.Message);
				}
			}
			string array;
			if (text == "auth")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url8 = Term.ReadLine();
					Term.Write("POST:");
					string param = Term.ReadLine();
					Term.WriteLine(Http.HttpPost(url8, param, PostHost, out array));
				}
				catch (Exception ex9)
				{
					Term.WriteLine(ex9.Message);
				}
			}
			if (text == "authpath")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url9 = Term.ReadLine();
					Term.Write("PATH:");
					string filePath3 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost(url9, ReadJson(filePath3), PostHost, out array));
				}
				catch (Exception ex10)
				{
					Term.WriteLine(ex10.Message);
				}
			}
			if (text == "peauth")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url10 = Term.ReadLine();
					Term.Write("POST:");
					string param2 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost_(url10, param2, PostHost));
				}
				catch (Exception ex11)
				{
					Term.WriteLine(ex11.Message);
				}
			}
			if (text == "peauthpath")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url11 = Term.ReadLine();
					Term.Write("PATH:");
					string filePath4 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost_(url11, ReadJson(filePath4), PostHost));
				}
				catch (Exception ex12)
				{
					Term.WriteLine(ex12.Message);
				}
			}
			if (text == "authv2")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url12 = Term.ReadLine();
					Term.Write("POST:");
					string param3 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost_v2(url12, param3, PostHost, out array));
				}
				catch (Exception ex13)
				{
					Term.WriteLine(ex13.Message);
				}
			}
			if (text == "authpathv2")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url13 = Term.ReadLine();
					Term.Write("PATH:");
					string filePath5 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost_v2(url13, ReadJson(filePath5), PostHost, out array));
				}
				catch (Exception ex14)
				{
					Term.WriteLine(ex14.Message);
				}
			}
			if (text == "peauthv2")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url14 = Term.ReadLine();
					Term.Write("POST:");
					string param4 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost_v2_(url14, param4, PostHost));
				}
				catch (Exception ex15)
				{
					Term.WriteLine(ex15.Message);
				}
			}
			if (text == "peauthpathv2")
			{
				try
				{
					Term.WriteLine("default server:{0} It can be changed via \"setposthost\" command", PostHost);
					Term.Write("URL:");
					string url15 = Term.ReadLine();
					Term.Write("PATH:");
					string filePath6 = Term.ReadLine();
					Term.WriteLine(Http.HttpPost_v2_(url15, ReadJson(filePath6), PostHost));
				}
				catch (Exception ex16)
				{
					Term.WriteLine(ex16.Message);
				}
			}
			if (text == "setposthost")
			{
				Term.Write("PostHost:");
				PostHost = Term.ReadLine();
			}
			if (text == "ajoin")
			{
				if (sajoin)
				{
					Term.WriteLine("免责声明：假人仅供技术讨论，房间引流，请勿用于非法用途，锁服造成的一切后果均为使用者自行承担！使用请接受条款（y/n）");
					Term.Write("是否接受条款：");
					string text11 = Term.ReadLine();
					if (text11 == "Y" || text11 == "y")
					{
						if (!start_stop)
						{
							try
							{
								start_stop = true;
								Term.WriteLine("谨慎使用");
								Term.Write("输入房间号：");
								string text12 = Term.ReadLine();
								Http.istrue = false;
								string value = Http.postAPIPC("{\"room_name\":\"" + text12 + "\",\"res_id\":\"\",\"version\":\"" + Temp.Version + "\",\"offset\":0,\"length\":50}", "/online-lobby-room/query/search-by-name");
								Thread.Sleep(1000);
								JObject jObject2 = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(value)["entities"][0].ToString());
								Http.PurchaseItemPC(jObject2["res_id"].ToString());
								Ajoin(jObject2["entity_id"].ToString());
							}
							catch (Exception ex17)
							{
								Term.WriteLine(ex17.Message);
							}
						}
						else
						{
							Term.WriteLine("有任务正在执行！");
						}
					}
				}
				else
				{
					Term.WriteLine("该功能已被禁用！");
				}
			}
			if (text == "ajoinrid")
			{
				if (sajoin)
				{
					Term.WriteLine("免责声明：假人仅供技术讨论，房间引流，请勿用于非法用途，锁服造成的一切后果均为使用者自行承担！使用请接受条款（y/n）");
					Term.Write("是否接受条款：");
					string text13 = Term.ReadLine();
					if (text13 == "Y" || text13 == "y")
					{
						try
						{
							if (!start_stop)
							{
								start_stop = true;
								Term.WriteLine("谨慎使用");
								Term.Write("输入房间entity_id：");
								Ajoin(Term.ReadLine());
							}
							else
							{
								Term.WriteLine("有任务正在执行！");
							}
						}
						catch (Exception ex18)
						{
							Term.WriteLine(ex18.Message);
						}
					}
				}
				else
				{
					Term.WriteLine("该功能已被禁用！");
				}
			}
			if (text == "STOP")
			{
				start_stop = false;
			}
			if (text == "adduser")
			{
				LoginInfo.adduser();
			}
			if (text == "startr")
			{
				Term.Write("输入物品id：");
				string text14 = Term.ReadLine();
				Term.Write("输入最大人数：");
				string s = Term.ReadLine();
				try
				{
					SavePermissions = "true";
					Http.istrue = true;
					string value2 = Http.postAPIPC("{\"room_name\":\"" + Function.GetName(player_info) + "\",\"max_count\":" + int.Parse(s) + ",\"visibility\":0,\"res_id\":\"" + text14 + "\",\"save_id\":\"\",\"password\":\"\"}", "/online-lobby-room");
					Thread.Sleep(1000);
					Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(value2)["entity"].ToString())["entity_id"].ToString();
					updateroom(Http.room_id_public);
					Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
				}
				catch (Exception ex19)
				{
					Term.WriteLine(ex19.Message);
				}
			}
			if (text == "startrPE")
			{
				Term.Write("输入物品id：");
				string text15 = Term.ReadLine();
				Term.Write("输入最大人数：");
				string s2 = Term.ReadLine();
				try
				{
					SavePermissions = "true";
					Http.istrue = true;
					string value3 = Http.postAPI("{\"room_name\":\"" + Function.GetName(player_info) + "\",\"max_count\":" + int.Parse(s2) + ",\"visibility\":0,\"res_id\":\"" + text15 + "\",\"save_id\":\"\",\"password\":\"\"}", "/online-lobby-room");
					Thread.Sleep(1000);
					Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(value3)["entity"].ToString())["entity_id"].ToString();
					updateroom(Http.room_id_public);
					Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
				}
				catch (Exception ex20)
				{
					Term.WriteLine(ex20.Message);
				}
			}
			if (text == "visitorsave")
			{
				Term.Write("是否允许保存(y,n)？：");
				string text16 = Term.ReadLine();
				if (text16 == "y")
				{
					Http.postAPIPC("{\"visibility\":" + JoinPermissions + ",\"allow_save\":true,\"owner_id\":" + Http.UID + ",\"room_id\":\"" + Http.room_id_public + "\",\"entity_id\":null}", "/online-lobby-room/update");
					SavePermissions = "true";
				}
				else if (text16 == "n")
				{
					Http.postAPIPC("{\"visibility\":" + JoinPermissions + ",\"allow_save\":false,\"owner_id\":" + Http.UID + ",\"room_id\":\"" + Http.room_id_public + "\",\"entity_id\":null}", "/online-lobby-room/update");
					SavePermissions = "false";
				}
				else
				{
					Term.WriteLine("输入错误");
				}
			}
			if (text == "save")
			{
				Term.Write("输入保存名称：");
				string text17 = Term.ReadLine();
				Http.postAPIPC("{\"name\":\"" + text17 + "\",\"backup_id\":\"1\"}", "/online-lobby-backup/create");
			}
			if (text == "saveadd")
			{
				Term.Write("输入保存名称：");
				string text18 = Term.ReadLine();
				Term.Write("保存num：");
				string text19 = Term.ReadLine();
				Http.postAPIPC("{\"name\":\"" + text18 + "\",\"backup_id\":\"" + text19 + "\"}", "/online-lobby-backup/create");
			}
			if (text == "setpassword")
			{
				Term.Write("输入新的密码（留空为无密码）：");
				string text20 = Term.ReadLine();
				Http.postAPIPC("{\"visibility\":" + JoinPermissions + ",\"allow_save\":false,\"owner_id\":" + Http.UID + ",\"room_id\":\"" + Http.room_id_public + "\",\"entity_id\":null,\"password\":\"" + text20 + "\"}", "/online-lobby-room/update");
			}
			if (text == "kickall")
			{
				try
				{
					RoomUserList roomUserList = new RoomUserList();
					roomUserList = JsonConvert.DeserializeObject<RoomUserList>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-member/query/list-by-room-id", "{\"room_id\":\"" + Http.room_id_public + "\"}"));
					long[] array2 = new long[roomUserList.entities.Length];
					for (int i = 0; i < roomUserList.entities.Length; i++)
					{
						array2[i] = long.Parse(roomUserList.entities[i].member_id);
					}
					for (int j = 0; j < array2.Length; j++)
					{
						if (array2[j] != long.Parse(Http.UID) && array2[j] != long.Parse(Http.PEUID))
						{
							Http.KickAsync(array2[j].ToString());
						}
					}
				}
				catch (Exception ex21)
				{
					Term.WriteLine(ex21.ToString());
				}
			}
			if (text == "load")
			{
				try
				{
					SavePermissions = "true";
					Http.istrue = true;
					JObject jObject3 = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.getAPIPC("/online-lobby-backup/query/list-by-user"))["entities"][0].ToString());
					string text21 = jObject3["res_id"].ToString();
					string text22 = jObject3["save_id"].ToString();
					Term.Write("输入最大人数：");
					string s3 = Term.ReadLine();
					Term.WriteLine("save_id:" + text22);
					Term.WriteLine("res_id:" + text21);
					string text23 = "{\"room_name\":\"" + Function.GetName(player_info) + "\",\"max_count\":" + int.Parse(s3) + ",\"visibility\":0,\"res_id\":\"" + text21 + "\",\"save_id\":\"" + text22 + "\",\"password\":\"\"}";
					Term.WriteLine(text23);
					Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.postAPIPC(text23, "/online-lobby-room"))["entity"].ToString())["entity_id"].ToString();
					updateroom(Http.room_id_public);
					Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
				}
				catch (Exception ex22)
				{
					Term.WriteLine(ex22.Message);
				}
			}
			if (text == "loadPE")
			{
				try
				{
					SavePermissions = "true";
					Http.istrue = true;
					JObject jObject4 = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.getAPIPC("/online-lobby-backup/query/list-by-user"))["entities"][0].ToString());
					string text24 = jObject4["res_id"].ToString();
					string text25 = jObject4["save_id"].ToString();
					Term.Write("输入最大人数：");
					string s4 = Term.ReadLine();
					Term.WriteLine("save_id:" + text25);
					Term.WriteLine("res_id:" + text24);
					string text26 = "{\"room_name\":\"" + Function.GetName(player_info) + "\",\"max_count\":" + int.Parse(s4) + ",\"visibility\":0,\"res_id\":\"" + text24 + "\",\"save_id\":\"" + text25 + "\",\"password\":\"\"}";
					Term.WriteLine(text26);
					Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.postAPI(text26, "/online-lobby-room"))["entity"].ToString())["entity_id"].ToString();
					updateroom(Http.room_id_public);
					Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
				}
				catch (Exception ex23)
				{
					Term.WriteLine(ex23.Message);
				}
			}
			if (text == "loadadd")
			{
				try
				{
					SavePermissions = "true";
					Http.istrue = true;
					JObject jObject5 = JsonConvert.DeserializeObject<JObject>(Http.getAPIPC("/online-lobby-backup/query/list-by-user"));
					jObject5["entities"].ToString();
					Term.Write("输入序号（从零开始）：");
					int num = int.Parse(Term.ReadLine());
					JObject jObject6 = JsonConvert.DeserializeObject<JObject>(jObject5["entities"][num].ToString());
					string text27 = jObject6["res_id"].ToString();
					string text28 = jObject6["save_id"].ToString();
					Term.Write("输入最大人数：");
					string s5 = Term.ReadLine();
					Term.WriteLine("save_id:" + text27);
					Term.WriteLine("res_id:" + text28);
					string text29 = "{\"room_name\":\"" + Function.GetName(player_info) + "\",\"max_count\":" + int.Parse(s5) + ",\"visibility\":0,\"res_id\":\"" + text27 + "\",\"save_id\":\"" + text28 + "\",\"password\":\"\"}";
					Term.WriteLine(text29);
					string value4 = Http.postAPIPC(text29, "/online-lobby-room");
					Http.istrue = true;
					Http.room_id_public = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(value4)["entity"].ToString())["entity_id"].ToString();
					updateroom(Http.room_id_public);
					Http.postAPIPC("{\"op\":\"start\"}", "/online-lobby-game-control");
				}
				catch (Exception ex24)
				{
					Term.WriteLine(ex24.Message);
				}
			}
			if (text == "kick")
			{
				Term.Write("输入玩家UID：");
				Http.KickAsync(Term.ReadLine());
			}
			if (text == "visibility")
			{
				try
				{
					Term.WriteLine("0为公开");
					Term.WriteLine("1为仅好友可进");
					Term.WriteLine("2为隐藏");
					Term.Write("可见性：");
					JoinPermissions = int.Parse(Term.ReadLine());
					Http.postAPIPC("{\"visibility\":" + JoinPermissions + ",\"allow_save\":" + SavePermissions + ",\"owner_id\":" + Http.UID + ",\"room_id\":\"" + Http.room_id_public + "\",\"entity_id\":null}", "/online-lobby-room/update");
				}
				catch (Exception ex25)
				{
					Term.WriteLine(ex25.Message);
				}
			}
			if (text == "reset")
			{
				Http.postAPIPC("{\"op\":\"restart\"}", "/online-lobby-game-control");
			}
			if (text == "setop")
			{
				Term.Write("输入玩家UID：");
				string text30 = Term.ReadLine();
				Http.postAPIPC("{\"visibility\":" + JoinPermissions + ",\"allow_save\":" + SavePermissions + ",\"owner_id\":" + text30 + ",\"room_id\":\"" + Http.room_id_public + "\",\"entity_id\":null}", "/online-lobby-room/update");
			}
			if (text == "getrtxgame")
			{
				Http.postAPIPC("{\"os\":\"10.0\",\"version\":200000000,\"entity_id\":null}", "/cpp-game-client-info");
			}
			if (text == "getallgame")
			{
				Http.postAPIPC("{\"setting_name\":\"pc_sdkclient_full_patchlist\"}", "/interconn/web/pack-setting/get-for-mcstudio");
			}
			if (text == "test")
			{
				Http.postAPIPC("{\"os\":\"10.0\",\"version\":300000000,\"entity_id\":null}", "/cpp-game-client-info");
			}
			if (text == "searchuser")
			{
				Term.Write("输入用户名：");
				string text31 = Term.ReadLine();
				Http.postAPIPC("{\"name_or_mail\":\"" + text31 + "\",\"mail_flag\":false}", "/user-search-friend");
			}
			if (text == "startban")
			{
				Term.Write("输入房间entityid：");
				start_stop = true;
				AKICK(Term.ReadLine());
				Term.WriteLine("已启动ban");
			}
			if (text == "ban")
			{
				Term.Write("输入玩家uid：");
				string text32 = Term.ReadLine();
				string[] str = Temp.ban_id.ToArray();
				if (Temp.ban_id.ToArray().Count() == 0 || checkplayer(str, text32))
				{
					Temp.ban_id.Add(text32);
					File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
					Temp = JsonConvert.DeserializeObject<temp>(ReadJson("config.json"));
					Term.WriteLine("已添加:" + text32);
					Http.postAPIPC("{\"room_id\":\"" + Http.room_id_public + "\",\"user_id\":" + text32 + "}", "/online-lobby-member-kick");
				}
				else
				{
					Term.WriteLine("已添加过垓玩家");
				}
			}
			if (text == "unban")
			{
				Term.Write("输入玩家uid：");
				string item2 = Term.ReadLine();
				if (Temp.ban_id.Remove(item2))
				{
					Term.WriteLine("成功移除");
					File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
				}
				else
				{
					Term.WriteLine("未找到该玩家");
				}
			}
			if (text == "playerlist")
			{
				try
				{
					RoomUserList roomUserList2 = new RoomUserList();
					roomUserList2 = JsonConvert.DeserializeObject<RoomUserList>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-member/query/list-by-room-id", "{\"room_id\":\"" + Http.room_id_public + "\"}"));
					uint[] array3 = new uint[roomUserList2.entities.Length];
					for (int k = 0; k < roomUserList2.entities.Length; k++)
					{
						array3[k] = uint.Parse(roomUserList2.entities[k].member_id);
					}
					RoomUserNameListRequest roomUserNameListRequest = new RoomUserNameListRequest();
					roomUserNameListRequest.entity_ids = array3;
					JObject jObject7 = JsonConvert.DeserializeObject<JObject>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/user/query/search-by-ids", JsonConvert.SerializeObject(roomUserNameListRequest)));
					for (int l = 0; l < roomUserList2.entities.Length; l++)
					{
						Term.WriteLine(jObject7["entities"][l]["entity_id"]?.ToString() + "：" + jObject7["entities"][l]["name"].ToString());
					}
				}
				catch
				{
				}
			}
			if (text == "wplayerlist")
			{
				Term.Write("输入房间entityid：");
				string text33 = Term.ReadLine();
				try
				{
					RoomUserList roomUserList3 = new RoomUserList();
					roomUserList3 = JsonConvert.DeserializeObject<RoomUserList>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/online-lobby-member/query/list-by-room-id", "{\"room_id\":\"" + text33 + "\"}"));
					uint[] array4 = new uint[roomUserList3.entities.Length];
					for (int m = 0; m < roomUserList3.entities.Length; m++)
					{
						array4[m] = uint.Parse(roomUserList3.entities[m].member_id);
					}
					RoomUserNameListRequest roomUserNameListRequest2 = new RoomUserNameListRequest();
					roomUserNameListRequest2.entity_ids = array4;
					JObject jObject8 = JsonConvert.DeserializeObject<JObject>(Http.x19post("https://x19apigatewayobt.nie.netease.com", "/user/query/search-by-ids", JsonConvert.SerializeObject(roomUserNameListRequest2)));
					for (int n = 0; n < roomUserList3.entities.Length; n++)
					{
						Term.WriteLine(jObject8["entities"][n]["entity_id"]?.ToString() + "：" + jObject8["entities"][n]["name"].ToString());
					}
				}
				catch
				{
				}
			}
			if (text == "clearbanid")
			{
				Temp.ban_id.Clear();
				File.WriteAllText("config.json", JsonConvert.SerializeObject(Temp));
				Temp = JsonConvert.DeserializeObject<temp>(ReadJson("config.json"));
				Term.WriteLine("已清除");
			}
			if (text == "gametype")
			{
				if (CheckStart(3))
				{
					// 三段参数必须全部读完再判断，否则某一段解析失败时剩下的行会顶到下一条命令上。
					string aMode = null, aIp = null, aPort = null;
					try
					{
						Term.Write("multiplayer_game_type:");
						aMode = Term.ReadLine();
						Term.Write("IP:");
						aIp = Term.ReadLine();
						Term.Write("Port:");
						aPort = Term.ReadLine();
					}
					catch
					{
						Term.Discard(3, 0);
					}
					int multiplayer_game_type, port;
					if (!int.TryParse(aMode, out multiplayer_game_type) || string.IsNullOrWhiteSpace(aIp)
						|| !int.TryParse(aPort, out port))
					{
						Term.WriteLine("启动参数不完整（应为 模式 / IP / 端口 三段）。");
					}
					else
					{
						try
						{
							string contents3 = Function.game_(aIp.Trim(), port, multiplayer_game_type);
							File.WriteAllText(Temp.path + "\\mc.cfg", contents3);
							InvokeCmdAsync("start " + Temp.path + "\\Minecraft.Windows.exe");
						}
						catch
						{
							Term.WriteLine("路径错误，请检查路径是否存在！");
						}
					}
				}
			}
			if (text == "joinlobby")
			{
				try
				{
					string value5 = Http.postAPIHost("{\"version\":\"" + Temp.Version + "\"}", "/enter-main-city", "https://g79mclobt.minecraft.cn");
					string item3 = JsonConvert.DeserializeObject<JObject>(value5)["entity"]["res_id"].ToString();
					string ip2 = JsonConvert.DeserializeObject<JObject>(value5)["entity"]["server_host"].ToString();
					string s6 = JsonConvert.DeserializeObject<JObject>(value5)["entity"]["server_port"].ToString();
					CppGameConfig cppGameConfig2 = new CppGameConfig();
					cppGameConfig2.world_info.level_id = "114514";
					cppGameConfig2.room_info.ip = ip2;
					cppGameConfig2.room_info.port = int.Parse(s6);
					cppGameConfig2.room_info.token = CppGameM.GetLoginToken();
					cppGameConfig2.player_info.user_id = uint.Parse(Http.UID);
					cppGameConfig2.player_info.user_name = Function.GetName(player_info);
					cppGameConfig2.player_info.urs = Http.UID;
					cppGameConfig2.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
					cppGameConfig2.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
					cppGameConfig2.skin_info.skin_iid = "-1";
					cppGameConfig2.misc.multiplayer_game_type = 1000;
					cppGameConfig2.misc.auth_server_url = "https://g79authobt.nie.netease.com";
					cppGameConfig2.web_server_url = "https://x19apigatewayobt.nie.netease.com";
					cppGameConfig2.core_server_url = "https://x19obtcore.nie.netease.com:8443";
					cppGameConfig2.world_info.name = "114514";
					cppGameConfig2.room_info.item_ids.Add("4668698705152194374");
					cppGameConfig2.room_info.item_ids.Add(item3);
					string text34 = JsonConvert.SerializeObject(cppGameConfig2, Formatting.Indented);
					if (Temp.cppconfig_encryption)
					{
						text34 = text34.Encrypt();
					}
					Function.GameLeftLobbyAsync(text34);
				}
				catch
				{
				}
			}
			if (text == "portstart")
			{
				try
				{
					Term.Write("multiplayer_game_type:");
					int multiplayer_game_type2 = int.Parse(Term.ReadLine());
					Term.Write("IP:");
					string ip3 = Term.ReadLine();
					Term.Write("Port:");
					int port2 = int.Parse(Term.ReadLine());
					GameRpcPort = new RPCPort();
					CppGameConfig cppGameConfig3 = new CppGameConfig();
					cppGameConfig3.world_info.level_id = "114514";
					cppGameConfig3.room_info.ip = ip3;
					cppGameConfig3.room_info.port = port2;
					cppGameConfig3.room_info.token = CppGameM.GetLoginToken();
					cppGameConfig3.player_info.user_id = uint.Parse(Http.UID);
					cppGameConfig3.player_info.user_name = Function.GetName(player_info);
					cppGameConfig3.player_info.urs = Http.UID;
					cppGameConfig3.skin_info.skin = Directory.GetCurrentDirectory() + "\\steve.png";
					cppGameConfig3.skin_info.md5 = "e5a2e67dba2c2d6bb80817a1c9211036";
					cppGameConfig3.skin_info.skin_iid = "-1";
					cppGameConfig3.misc.multiplayer_game_type = multiplayer_game_type2;
					cppGameConfig3.misc.auth_server_url = "https://g79authobt.nie.netease.com";
					cppGameConfig3.misc.launcher_port = GameRpcPort.LauncherControlPort;
					cppGameConfig3.web_server_url = "https://x19apigatewayobt.nie.netease.com";
					cppGameConfig3.core_server_url = "https://x19obtcore.nie.netease.com:8443";
					cppGameConfig3.world_info.name = "114514";
					cppGameConfig3.room_info.item_ids = Temp.item_ids;
					string text35 = JsonConvert.SerializeObject(cppGameConfig3, Formatting.Indented);
					if (Temp.cppconfig_encryption)
					{
						text35 = text35.Encrypt();
					}
					Function.GameAsync(text35, GameRpcPort);
				}
				catch
				{
				}
			}
			if (text == "senddata")
			{
				try
				{
					if (GameRpcPort.CheckConnect())
					{
						Term.Write("DATA:");
						GameRpcPort.SendControlDataSRC(Encoding.ASCII.GetBytes(Term.ReadLine()));
					}
					else
					{
						Term.WriteLine("游戏还未启动");
					}
				}
				catch
				{
					Term.WriteLine("游戏还未启动");
				}
			}
			if (text == "startconnect")
			{
				try
				{
					if (GameRpcPort == null)
					{
						Term.Write("Port:");
						GameRpcPort = new RPCPort(int.Parse(Term.ReadLine()));
						Term.WriteLine(GameRpcPort.LauncherControlPort);
					}
				}
				catch (Exception ex26)
				{
					Term.WriteLine(ex26.Message);
				}
			}
			if (text == "javaconnect")
			{
				try
				{
					if (GameRpcPort == null)
					{
						Term.Write("Port:");
						string s7 = Term.ReadLine();
						Term.Write("IP:");
						Function.IP = Term.ReadLine();
						Term.Write("Port:");
						Function.Port = Term.ReadLine();
						Term.Write("Name:");
						Function.Name = Term.ReadLine();
						GameRpcPort = new RPCPort(int.Parse(s7));
						GameRpcPort.RegisterReceiveCallBack(517, Function.StartCheck);
						Term.WriteLine(GameRpcPort.LauncherControlPort);
					}
				}
				catch (Exception ex27)
				{
					Term.WriteLine(ex27.Message);
				}
			}
			if (text == "closeconnect")
			{
				try
				{
					GameRpcPort.NormalExit();
					GameRpcPort = null;
					Term.WriteLine("close");
				}
				catch
				{
					Term.WriteLine("游戏还未启动");
				}
			}
			if (text == "closeroomupdate")
			{
				isroom = false;
				Term.WriteLine("已关闭自动更新");
			}
			if (text == "searchuid")
			{
				Term.Write("输入UID：");
				Http.postAPIPC("{\"user_id\":\"" + Term.ReadLine() + "\"}", "/user/query/search-by-uid");
			}
			if (text == "sendjava")
			{
				try
				{
					if (GameRpcPort.CheckConnect())
					{
						Term.Write("GUID:");
						string gUID = Term.ReadLine();
						Term.Write("WorldName:");
						string worldName = Term.ReadLine();
						JavaGameConfig javaGameConfig = new JavaGameConfig();
						javaGameConfig.GUID = gUID;
						javaGameConfig.WorldName = worldName;
						new JavaMCGameM().StartExistGame(javaGameConfig, isLanGame: false);
					}
					else
					{
						Term.WriteLine("游戏还未启动");
					}
				}
				catch (Exception ex28)
				{
					Term.WriteLine(ex28.Message);
				}
			}
			if (text == "sendjavav2")
			{
				try
				{
					if (GameRpcPort.CheckConnect())
					{
						Term.Write("GUID:");
						string gUID2 = Term.ReadLine();
						Term.Write("WorldName:");
						string worldName2 = Term.ReadLine();
						JavaGameConfig javaGameConfig2 = new JavaGameConfig();
						javaGameConfig2.GUID = gUID2;
						javaGameConfig2.WorldName = worldName2;
						javaGameConfig2.GameMode = GameMode.SURVIVAL;
						javaGameConfig2.AllowCheat = true;
						javaGameConfig2.GenerateBuildings = true;
						javaGameConfig2.BonusChest = false;
						javaGameConfig2.MapType = MapType.DEFAULT;
						javaGameConfig2.Seed = "";
						new JavaMCGameM().StartExistGame(javaGameConfig2, isLanGame: false);
					}
					else
					{
						Term.WriteLine("游戏还未启动");
					}
				}
				catch (Exception ex29)
				{
					Term.WriteLine(ex29.Message);
				}
			}
			if (text == "getdtoken")
			{
				Term.WriteLine(Http.LoginDToken);
			}
			if (text == "chachareset")
			{
				GameRpcPort.startChaChaEnc();
			}
			if (text == "getuid")
			{
				Term.WriteLine(Http.UID);
			}
			if (text == "gettoken")
			{
				Term.WriteLine(Http.LoginSRCToken);
			}
			if (text == "getiteminfo")
			{
				try
				{
					Term.Write("ITEM_ID:");
					sd.j(Term.ReadLine());
				}
				catch (Exception ex30)
				{
					Term.WriteLine(ex30.Message);
				}
			}
			if (text == "searchpemod")
			{
				Term.Write("输入模组名称：");
				Http.postAPIHost("{\"offset\":0,\"length\":20,\"keyword\":\"" + Term.ReadLine() + "\",\"init\":0,\"first_type\":\"0\",\"channel_id\":5}\r\n", "/pe-item/query/search-by-keyword/", "https://g79mclobtgray.nie.netease.com");
			}
			if (text == "searchmod")
			{
				Term.Write("输入模组名称：");
				Http.postAPIPC("{\"item_type\":2,\"keyword\":\"" + Term.ReadLine() + "\",\"master_type_id\":\"0\",\"secondary_type_id\":\"0\",\"sort_type\":1,\"order\":0,\"offset\":0,\"length\":24,\"is_has\":true,\"year\":0,\"is_sync\":0,\"price_type\":0}", "/item/query/search-by-keyword");
			}
			if (text == "getpeuid")
			{
				Term.WriteLine(Http.PEUID);
			}
			if (text == "getpemod")
			{
				Term.Write("ITEM_ID:");
				Http.postAPIHost("{\"item_ids\":[\"" + Term.ReadLine() + "\"]}", "/pe-item/query/search-lobby-by-id-list", "https://g79mclobtgray.nie.netease.com");
			}
			if (text == "getmod")
			{
				Term.Write("ITEM_ID:");
				Http.postAPIPC("{\"item_id\":\"" + Term.ReadLine() + "\",\"length\":0,\"offset\":0}", "/user-item-download-v2");
			}
			if (text == "getitemkey")
			{
				try
				{
					Term.Write("ITEM_ID:");
					string text36 = Term.ReadLine();
					string randomKey = Function.GetRandomKey(32);
					string text37 = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.EncyptPOST("/pe-item/get-encryption-key-list-for-guests", "{\"device_id\":\"" + randomKey + "\",\"item_ids\":[\"" + text36 + "\"]}", "https://x19obtcore.nie.netease.com:8443"))["entities"][0].ToString())["jwt"].ToString().Remove(0, 37);
					string text38 = text37.Remove(text37.IndexOf('.'));
					if (text38.Length % 4 == 3)
					{
						text38 += "=";
					}
					if (text38.Length % 4 == 2)
					{
						text38 += "==";
					}
					string value6 = Encoding.ASCII.GetString(Convert.FromBase64String(text38));
					Term.WriteLine("KEY：" + Function.GetDecryptionKey(randomKey, JsonConvert.DeserializeObject<JObject>(value6)["contentKey"].ToString(), Http.UID));
					Term.WriteLine("UUID：" + JsonConvert.DeserializeObject<JObject>(value6)["contentUuid"].ToString(), Http.UID);
				}
				catch (Exception ex31)
				{
					Term.WriteLine(ex31.Message);
				}
			}
			if (text == "getpeitemkey")
			{
				try
				{
					Term.Write("ITEM_ID:");
					string text39 = Term.ReadLine();
					string randomKey2 = Function.GetRandomKey(32);
					string text40 = JsonConvert.DeserializeObject<JObject>(JsonConvert.DeserializeObject<JObject>(Http.EncyptPOST("/pe-item/get-encryption-key-list-for-guests", "{\"device_id\":\"" + randomKey2 + "\",\"item_ids\":[\"" + text39 + "\"]}", "https://x19obtcore.nie.netease.com:8443"))["entities"][0].ToString())["jwt"].ToString().Remove(0, 37);
					string text41 = text40.Remove(text40.IndexOf('.'));
					if (text41.Length % 4 == 3)
					{
						text41 += "=";
					}
					if (text41.Length % 4 == 2)
					{
						text41 += "==";
					}
					string value7 = Encoding.ASCII.GetString(Convert.FromBase64String(text41));
					Term.WriteLine("KEY：" + Function.GetDecryptionKey(randomKey2, JsonConvert.DeserializeObject<JObject>(value7)["contentKey"].ToString(), Http.UID));
					Term.WriteLine("UUID：" + JsonConvert.DeserializeObject<JObject>(value7)["contentUuid"].ToString(), Http.UID);
				}
				catch (Exception ex32)
				{
					Term.WriteLine(ex32.Message);
				}
			}
			if (text == "ununzip")
			{
				Term.Write("path:");
				string path = Term.ReadLine();
				Term.Write("key:");
				string key = Term.ReadLine();
				Term.Write("uuid:");
				string uuid = Term.ReadLine();
				Function.UnUnzipJson(key, path, uuid);
			}
			if (text == "ununzippath")
			{
				Term.Write("path:");
				string path2 = Term.ReadLine();
				Term.Write("key:");
				string key2 = Term.ReadLine();
				Term.Write("uuid:");
				string uuid2 = Term.ReadLine();
				Function.UnUnzipJsonPath(path2, key2, uuid2);
			}
			if (text == "unzipmodjsonpath")
			{
				Term.Write("path:");
				string path3 = Term.ReadLine();
				Term.Write("key:");
				string key3 = Term.ReadLine();
				Term.Write("uuid:");
				string uuid3 = Term.ReadLine();
				Function.UnzipModJsonMCZIP(path3, key3, uuid3);
			}
			if (text == "decryptpath")
			{
				Term.Write("path:");
				string path4 = Term.ReadLine();
				Term.Write("key:");
				string key4 = Term.ReadLine();
				Term.Write("uuid:");
				string uuid4 = Term.ReadLine();
				Function.UnzipModJsonPath(path4, key4, uuid4);
			}
			if (text == "startserver")
			{
				try
				{
					Term.Write("输入认证服务器端口：");
					string text42 = Term.ReadLine();
					Http.httpobj = new HttpListener();
					string text43 = "http://127.0.0.1:" + text42 + "/";
					Http.httpobj.Prefixes.Add(text43);
					Http.httpobj.Start();
					new Thread(() =>
					{
						while (true)
						{
							Http.Result();
						}
					}).Start();
					Http.AuthServerUrl = text43;
					Term.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
				}
				catch (Exception ex33)
				{
					Term.WriteLine("未经处理的异常：" + ex33.ToString());
				}
			}
			if (text == "closeencrypt")
			{
				Http.EncryptionAuthentication = false;
				Term.WriteLine("已关闭加密");
			}
			if (text == "startencrypt")
			{
				Http.EncryptionAuthentication = true;
				Term.WriteLine("已开启加密");
			}
			if (text == "startserverv2")
			{
				try
				{
					Term.Write("输入认证服务器端口：");
					string text44 = Term.ReadLine();
					Http.httpobj = new HttpListener();
					string text45 = "http://127.0.0.1:" + text44 + "/";
					Http.httpobj.Prefixes.Add(text45);
					Http.httpobj.Start();
					new Thread(() =>
					{
						while (true)
						{
							Http.Result_v2();
						}
					}).Start();
					Http.AuthServerUrl = text45;
					Term.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
				}
				catch (Exception ex34)
				{
					Term.WriteLine("未经处理的异常：" + ex34.ToString());
				}
			}
			if (text == "startserverv3")
			{
				try
				{
					Term.Write("输入认证服务器端口：");
					string text46 = Term.ReadLine();
					Http.httpobj = new HttpListener();
					string text47 = "http://127.0.0.1:" + text46 + "/";
					Http.httpobj.Prefixes.Add(text47);
					Http.httpobj.Start();
					new Thread(() =>
					{
						while (true)
						{
							Http.Result_v3();
						}
					}).Start();
					Http.AuthServerUrl = text47;
					Term.WriteLine("服务端初始化完毕，正在等待客户端请求,时间：" + DateTime.Now.ToString() + "\r\n");
				}
				catch (Exception ex35)
				{
					Term.WriteLine("未经处理的异常：" + ex35.ToString());
				}
			}
			if (text == "serverid")
			{
				Term.Write("id:");
				Http.server_id = Term.ReadLine();
			}
			if (text == "serverip")
			{
				Term.Write("ip:");
				Http.server_ip = Term.ReadLine();
			}
			if (text == "cuccserver")
			{
				Term.WriteLine("默认服务器：https://g79.signal.servertransferser");
				Term.WriteLine(new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://impression.update.netease.com/lighten/atlas_g79_hangzhou-cucc.txt")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd());
			}
			if (text == "ctccserver")
			{
				Term.WriteLine("默认服务器：https://g79.signal.servertransferser");
				Term.WriteLine(new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://impression.update.netease.com/lighten/atlas_g79_hangzhou-ctcc.txt")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd());
			}
			if (text == "cmccserver")
			{
				Term.WriteLine("默认服务器：https://g79.signal.servertransferser");
				Term.WriteLine(new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://impression.update.netease.com/lighten/atlas_g79_hangzhou-cmcc.txt")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd());
			}
			if (text == "allserver")
			{
				Term.WriteLine("默认服务器：https://g79.signal.servertransferser");
				Term.WriteLine(new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://g79.update.netease.com/transferserver_obt_new.list")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd());
			}
			if (text == "mkick")
			{
				try
				{
					if (GameRpcPort.CheckConnect())
					{
						Term.Write("玩家uid:");
						string s8 = Term.ReadLine();
						GameRpcPort.SendControlData(SimplePack.Pack((ushort)1297, BitConverter.GetBytes(uint.Parse(s8))));
					}
					else
					{
						Term.WriteLine("游戏还未启动");
					}
				}
				catch
				{
					Term.WriteLine("游戏还未启动");
				}
			}
			if (text == "startroom")
			{
				if (Temp.path == "")
				{
					Term.WriteLine("你还没有设置游戏路径！");
				}
				else
				{
					try
					{
						string contents4 = Function.game();
						File.WriteAllText(Temp.path + "\\mc.cfg", contents4);
						InvokeCmdAsync("start " + Temp.path + "\\Minecraft.Windows.exe");
					}
					catch
					{
						Term.WriteLine("路径错误，请检查路径是否存在！");
					}
				}
			}
			if (text == "traceUID")
			{
				try
				{
					if (!start_stop)
					{
						Term.Write("输入uid：");
						Function.TraceUser(Term.ReadLine());
					}
				}
				catch (Exception ex36)
				{
					Term.WriteLine(ex36.ToString());
				}
			}
			if (text == "startlinkconnect" && (!LinkConnection.Is_Connected || linkConnection == null))
			{
				linkConnection = new LinkConnection();
			}
			if (text == "closelinkconnect")
			{
				linkConnection.Close();
				Term.WriteLine("已关闭link");
			}
			if (text == "linkserver")
			{
				Term.WriteLine(new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://g79.update.netease.com/linkserver_obt.list")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd());
			}
			if (text == "linkserverexpr")
			{
				Term.WriteLine(new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://x19.update.netease.com/linkserver_expr.list")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd());
			}
			if (text == "newchacha")
			{
				try
				{
					Term.Write("输入key：");
					ChaCha8 chaCha = new ChaCha8(Term.ReadLine().HexToBytes());
					Term.Write("输入data：");
					byte[] array5 = Term.ReadLine().HexToBytes();
					chaCha.Process(array5);
					chaCha.Delete();
					Term.WriteLine(array5.ToHex());
				}
				catch
				{
				}
			}
			if (text == "newchacha_")
			{
				Term.Write("输入key：");
				string hex = Term.ReadLine();
				ChaCha8 chaCha2 = new ChaCha8(hex.HexToBytes());
				try
				{
					Term.WriteLine("输入left退出");
					while (true)
					{
						Term.Write("输入data：");
						hex = Term.ReadLine();
						if (hex == "left")
						{
							break;
						}
						byte[] array6 = hex.HexToBytes();
						chaCha2.Process(array6);
						Term.WriteLine(array6.ToHex());
					}
					chaCha2.Delete();
				}
				catch
				{
					chaCha2.Delete();
				}
			}
			if (text == "newchachafile")
			{
				try
				{
					Term.Write("输入key：");
					ChaCha8 chaCha3 = new ChaCha8(Term.ReadLine().HexToBytes());
					Term.Write("输入path：");
					byte[] array7 = ReadJson(Term.ReadLine()).HexToBytes();
					chaCha3.Process(array7);
					chaCha3.Delete();
					Term.WriteLine(array7.ToHex());
				}
				catch
				{
				}
			}
			if (text == "newchachafile_")
			{
				Term.Write("输入key：");
				string hex2 = Term.ReadLine();
				ChaCha8 chaCha4 = new ChaCha8(hex2.HexToBytes());
				try
				{
					bool flag2 = true;
					bool flag3 = true;
					bool flag4 = true;
					Term.WriteLine("输入hex切换输出模式");
					Term.WriteLine("输入zlib解密加解压");
					Term.WriteLine("输入left退出");
					while (true)
					{
						Term.Write("输入path：");
						hex2 = Term.ReadLine();
						switch (hex2)
						{
						case "hex":
							flag4 = false;
							flag2 = !flag2;
							Term.WriteLine(flag2);
							break;
						case "zlib":
							flag4 = false;
							flag3 = !flag3;
							Term.WriteLine(flag3);
							break;
						case "fast":
						{
							flag4 = false;
							Term.Write("$输入path：");
							hex2 = Term.ReadLine();
							byte[] array8 = ReadJson(hex2).HexToBytes();
							int num3;
							int size;
							for (int num2 = 0; array8.Length >= num2 + 2; num2 += num3 + size)
							{
								num3 = LinkConnection.BytesToVarInt(array8.Skip(num2).ToArray(), out size);
								if (array8.Length < num2 + num3 + size)
								{
									break;
								}
								byte[] array9 = new byte[num3];
								Array.Copy(array8, num2 + size, array9, 0, num3);
								chaCha4.Process(array9);
								if (flag2)
								{
									if (!flag3)
									{
										Term.WriteLine(ZlibUtils.MicrosoftDecompress(array9).ToHex());
									}
									else
									{
										Term.WriteLine(array9.ToHex());
									}
								}
								else if (!flag3)
								{
									Term.WriteLine(Encoding.UTF8.GetString(ZlibUtils.MicrosoftZlibDecompress(array9)));
								}
								else
								{
									Term.WriteLine(Encoding.UTF8.GetString(array9));
								}
							}
							break;
						}
						default:
							flag4 = true;
							break;
						}
						if (!flag4)
						{
							continue;
						}
						byte[] array10 = ReadJson(hex2).HexToBytes();
						chaCha4.Process(array10);
						if (flag2)
						{
							if (!flag3)
							{
								Term.WriteLine(ZlibUtils.MicrosoftDecompress(array10).ToHex());
							}
							else
							{
								Term.WriteLine(array10.ToHex());
							}
						}
						else if (!flag3)
						{
							Term.WriteLine(Encoding.UTF8.GetString(ZlibUtils.MicrosoftZlibDecompress(array10)));
						}
						else
						{
							Term.WriteLine(Encoding.UTF8.GetString(array10));
						}
					}
				}
				catch (Exception ex37)
				{
					Term.WriteLine(ex37.ToString());
				}
			}
			if (text == "fast")
			{
				try
				{
					Term.Write("输入物品id：");
					string text48 = Term.ReadLine();
					Http.room_id_public = JsonConvert.DeserializeObject<JObject>(Http.x19post("https://g79mclobt.minecraft.cn", "/online-lobby-room-match", "{\"version\": \"" + Temp.Version + "\", \"res_id\": \"" + text48 + "\", \"lobby_manifest_version\": \"\"}"))["entity"]["room_id"].ToString();
					Http.GetIP();
				}
				catch
				{
				}
			}
			if (text == "getsign")
			{
				Term.Write("输入message：");
				Term.WriteLine(Function.Base64Encode(CoreSign.PESignCount(Term.ReadLine())));
			}
			if (text == "startchatconnect" && !ChatConnection.Is_Connected)
			{
				chatConnection = new ChatConnection();
			}
			if (text == "closechatconnect")
			{
				chatConnection.Close();
			}
			if (text == "senduid" && ChatConnection.Is_Connected)
			{
				Term.Write("uid:");
				string text49 = Term.ReadLine();
				Term.Write("message:");
				string text50 = Term.ReadLine();
				string text51 = "[" + text49 + ",\"" + text50 + "\"]";
				Term.WriteLine(text51);
				byte[] bytes = Encoding.UTF8.GetBytes(text51);
				byte[] array11 = new byte[bytes.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)1281), 0, array11, 0, 2);
				Array.Copy(bytes, 0, array11, 2, bytes.Length);
				chatConnection.Encryption.Process(array11);
				byte[] array12 = new byte[array11.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)array11.Length), 0, array12, 0, 2);
				Array.Copy(array11, 0, array12, 2, array11.Length);
				byte[] array13 = new byte[array12.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)array12.Length), 0, array13, 0, 2);
				Array.Copy(array12, 0, array13, 2, array12.Length);
				chatConnection.networkStream.Write(array13, 0, array13.Length);
			}
			if (text == "sendlink")
			{
				try
				{
					Term.Write("message_hex:");
					string hex3 = Term.ReadLine();
					linkConnection.SendMsg(hex3.HexToBytes());
				}
				catch
				{
				}
			}
			if (text == "sendlinkfile")
			{
				try
				{
					Term.Write("message_path:");
					string filePath7 = Term.ReadLine();
					linkConnection.SendMsg(ReadJson(filePath7).HexToBytes());
				}
				catch
				{
				}
			}
			if (text == "userlist")
			{
				try
				{
					Term.Write("输入文件夹路径：");
					string path5 = Term.ReadLine();
					DummyUserList dummyUserList = new DummyUserList();
					dummyUserList.UserList = Function.GetAllFiles(path5, "");
					Function.SaveFile("DummyUserList.json", JsonConvert.SerializeObject(dummyUserList));
					Term.WriteLine("已保存文件：DummyUserList.json");
				}
				catch
				{
				}
			}
			if (text == "asenduid" && (ChatConnection.Is_Connected || start_stop))
			{
				Term.Write("uid:");
				string uid3 = Term.ReadLine();
				Term.Write("message:");
				string message = Term.ReadLine();
				start_stop = true;
				new Thread(() =>
				{
					while (start_stop)
					{
						byte[] bytes3 = Encoding.UTF8.GetBytes("[" + uid3 + ",\"" + message + "\"]");
						byte[] array19 = new byte[bytes3.Length + 2];
						Array.Copy(BitConverter.GetBytes((ushort)1281), 0, array19, 0, 2);
						Array.Copy(bytes3, 0, array19, 2, bytes3.Length);
						chatConnection.Encryption.Process(array19);
						byte[] array20 = new byte[array19.Length + 2];
						Array.Copy(BitConverter.GetBytes((ushort)array19.Length), 0, array20, 0, 2);
						Array.Copy(array19, 0, array20, 2, array19.Length);
						byte[] array21 = new byte[array20.Length + 2];
						Array.Copy(BitConverter.GetBytes((ushort)array20.Length), 0, array21, 0, 2);
						Array.Copy(array20, 0, array21, 2, array20.Length);
						chatConnection.networkStream.Write(array21, 0, array21.Length);
						Thread.Sleep(2000);
					}
				}).Start();
			}
			if (text == "senduidpath" && ChatConnection.Is_Connected)
			{
				Term.Write("uid:");
				string text52 = Term.ReadLine();
				Term.Write("path:");
				string text53 = ReadJson(Term.ReadLine());
				string text54 = "[" + text52 + ",\"" + text53 + "\"]";
				Term.WriteLine(text54);
				byte[] bytes2 = Encoding.UTF8.GetBytes(text54);
				byte[] array14 = new byte[bytes2.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)1281), 0, array14, 0, 2);
				Array.Copy(bytes2, 0, array14, 2, bytes2.Length);
				chatConnection.Encryption.Process(array14);
				byte[] array15 = new byte[array14.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)array14.Length), 0, array15, 0, 2);
				Array.Copy(array14, 0, array15, 2, array14.Length);
				byte[] array16 = new byte[array15.Length + 2];
				Array.Copy(BitConverter.GetBytes((ushort)array15.Length), 0, array16, 0, 2);
				Array.Copy(array15, 0, array16, 2, array15.Length);
				chatConnection.networkStream.Write(array16, 0, array16.Length);
			}
			if (text == "asenduidpath" && (ChatConnection.Is_Connected || start_stop))
			{
				Term.Write("uid:");
				string uid4 = Term.ReadLine();
				Term.Write("path:");
				string message2 = ReadJson(Term.ReadLine());
				start_stop = true;
				new Thread(() =>
				{
					while (start_stop)
					{
						byte[] bytes3 = Encoding.UTF8.GetBytes("[" + uid4 + ",\"" + message2 + "\"]");
						byte[] array19 = new byte[bytes3.Length + 2];
						Array.Copy(BitConverter.GetBytes((ushort)1281), 0, array19, 0, 2);
						Array.Copy(bytes3, 0, array19, 2, bytes3.Length);
						chatConnection.Encryption.Process(array19);
						byte[] array20 = new byte[array19.Length + 2];
						Array.Copy(BitConverter.GetBytes((ushort)array19.Length), 0, array20, 0, 2);
						Array.Copy(array19, 0, array20, 2, array19.Length);
						byte[] array21 = new byte[array20.Length + 2];
						Array.Copy(BitConverter.GetBytes((ushort)array20.Length), 0, array21, 0, 2);
						Array.Copy(array20, 0, array21, 2, array20.Length);
						chatConnection.networkStream.Write(array21, 0, array21.Length);
						Thread.Sleep(2000);
					}
				}).Start();
			}
			if (text == "addfriend")
			{
				Term.Write("uid:");
				string text55 = Term.ReadLine();
				Http.postAPIPC("{\"fid\":" + text55 + ",\"comment\":\"" + displayName + "\",\"message\":\"\"}", "/user-apply-friend");
			}
			if (text == "recvfriend")
			{
				Term.Write("uid:");
				string text56 = Term.ReadLine();
				Http.postAPIPC("{\"fid\":" + text56 + ",\"accept\":true}", "/user-reply-friend");
			}
			if (text == "initdummy")
			{
				DummySrv.Init();
			}
			if (text == "addsrvuser" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					SrvDummyUserListsEntity srvDummyUserListsEntity = new SrvDummyUserListsEntity();
					Term.Write("输入uid：");
					srvDummyUserListsEntity.uid = Term.ReadLine();
					Term.Write("输入到期时间：");
					srvDummyUserListsEntity.overdue_time = int.Parse(Term.ReadLine());
					Term.Write("输入最大任务数量：");
					srvDummyUserListsEntity.max_num = int.Parse(Term.ReadLine());
					if (!DummySrv.dummyUsers.user_lists.Contains(srvDummyUserListsEntity))
					{
						DummySrv.dummyUsers.user_lists.Add(srvDummyUserListsEntity);
						Function.SaveFile("user_srv_dummy_list.json", JsonConvert.SerializeObject(DummySrv.dummyUsers));
						Term.WriteLine("已保存文件：user_srv_dummy_list.json");
					}
					else
					{
						Term.WriteLine("已添加过用户！");
					}
				}
				catch (Exception ex38)
				{
					Term.WriteLine(ex38.ToString());
				}
			}
			if (text == "deleteuser" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("输入uid：");
					string text57 = Term.ReadLine();
					for (int num4 = 0; DummySrv.dummyUsers.user_lists.Count > num4; num4++)
					{
						if (DummySrv.dummyUsers.user_lists[num4].uid == text57)
						{
							DummySrv.dummyUsers.user_lists.RemoveAt(num4);
							Function.SaveFile("user_srv_dummy_list.json", JsonConvert.SerializeObject(DummySrv.dummyUsers));
							Term.WriteLine("已保存文件：user_srv_dummy_list.json");
						}
					}
				}
				catch (Exception ex39)
				{
					Term.WriteLine(ex39.ToString());
				}
			}
			if (text == "setusertime" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("输入uid：");
					string text58 = Term.ReadLine();
					Term.Write("输入新的时间：");
					string s9 = Term.ReadLine();
					for (int num5 = 0; DummySrv.dummyUsers.user_lists.Count > num5; num5++)
					{
						if (DummySrv.dummyUsers.user_lists[num5].uid == text58)
						{
							if (DummySrv.dummyUsers.user_lists[num5].overdue_time + 86400 > int.Parse(Function.GetTimeStamp()))
							{
								DummySrv.dummyUsers.user_lists[num5].overdue_time = int.Parse(s9);
								Function.SaveFile("user_srv_dummy_list.json", JsonConvert.SerializeObject(DummySrv.dummyUsers));
								Term.WriteLine("已保存文件：user_srv_dummy_list.json");
							}
							else
							{
								DummySrv.dummyUsers.user_lists[num5].overdue_time = int.Parse(s9);
								DummySrv.dummyUsers.user_lists[num5].max_num = 1;
								Function.SaveFile("user_srv_dummy_list.json", JsonConvert.SerializeObject(DummySrv.dummyUsers));
								Term.WriteLine("续费时间超过到期时间1天，已自动清除卡槽");
								Term.WriteLine("已保存文件：user_srv_dummy_list.json");
							}
						}
					}
				}
				catch (Exception ex40)
				{
					Term.WriteLine(ex40.ToString());
				}
			}
			if (text == "setusertask" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("输入uid：");
					string text59 = Term.ReadLine();
					Term.Write("输入最大任务数量：");
					string s10 = Term.ReadLine();
					for (int num6 = 0; DummySrv.dummyUsers.user_lists.Count > num6; num6++)
					{
						if (DummySrv.dummyUsers.user_lists[num6].uid == text59)
						{
							DummySrv.dummyUsers.user_lists[num6].max_num = int.Parse(s10);
							Function.SaveFile("user_srv_dummy_list.json", JsonConvert.SerializeObject(DummySrv.dummyUsers));
							Term.WriteLine("已保存文件：user_srv_dummy_list.json");
						}
					}
				}
				catch (Exception ex41)
				{
					Term.WriteLine(ex41.ToString());
				}
			}
			if (text == "uidtimelist" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					for (int num7 = 0; DummySrv.dummyUsers.user_lists.Count > num7; num7++)
					{
						Term.WriteLine(DummySrv.dummyUsers.user_lists[num7].uid + " time:" + DummySrv.dummyUsers.user_lists[num7].overdue_time + " MaxTask:" + DummySrv.dummyUsers.user_lists[num7].max_num);
					}
				}
				catch (Exception ex42)
				{
					Term.WriteLine(ex42.ToString());
				}
			}
			if (text == "kickplayer")
			{
				try
				{
					Term.Write("输入uid：");
					string text60 = Term.ReadLine();
					int dummy_level = DummySrv.BotConfig.dummy_level;
					Random random = new Random();
					DummyUserList dummyUserList2 = new DummyUserList();
					RecvMessageEntity recvMessageEntity = new RecvMessageEntity();
					string[] array17 = new string[dummy_level];
					for (int num8 = 0; num8 < dummy_level; num8++)
					{
						string item4 = (array17[num8] = DummySrv.strings[random.Next(0, Enumerable.Count(DummySrv.strings))]);
						DummySrv.strings.Remove(item4);
					}
					dummyUserList2.UserList = array17;
					DummySrv.dummyClients.Add(DummySrv.StartDummy(text60, recvMessageEntity, dummyUserList2, "发起用户：" + Http.UID + "---目标用户：" + text60 + "---时间：9999分钟", 9999));
				}
				catch
				{
				}
			}
			if (text == "javagetname")
			{
				Term.Write("输入gameid：");
				string text61 = Term.ReadLine();
				Http.postAPIPC("\"offset\":0,\"length\":10,\"user_id\":\"" + Http.UID + "\",\"game_id\":\"" + text61 + "\",\"game_type\":2}", "/game-character/query/user-game-characters");
			}
			if (text == "javaddname")
			{
				Term.Write("输入gameid：");
				string text62 = Term.ReadLine();
				Term.Write("输入游戏昵称：");
				string text63 = Term.ReadLine();
				Http.postAPIPC("{\"entity_id\":\"0\",\"game_id\":\"" + text62 + "\",\"game_type\":2,\"user_id\":\"" + Http.UID + "\",\"name\":\"" + text63 + "\",\"create_time\":555555}", "/game-character");
			}
			if (text == "getuuid")
			{
				Term.Write("输入游戏昵称：");
				Term.WriteLine(JavaLauncher.GenerateUUID(Term.ReadLine()));
			}
			if (text == "reloadconfig")
			{
				try
				{
					ReloadConfig();
				}
				catch (Exception ex43)
				{
					Term.WriteLine(ex43.ToString());
				}
			}
			if (text == "lockserver" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				Term.Write("输入房间号：");
				string name = Term.ReadLine();
				Random random2 = new Random();
				string[] texts = new string[10];
				for (int num9 = 0; num9 < 10; num9++)
				{
					string text64 = DummySrv.strings[random2.Next(0, Enumerable.Count(DummySrv.strings))];
					texts[num9] = text64;
					DummySrv.strings.Remove(text64);
				}
				new Thread(() =>
				{
					new LockServer(texts, name);
				}).Start();
			}
			if (text == "lockserverv2")
			{
				Term.Write("输入ip：");
				string name2 = Term.ReadLine();
				Term.Write("输入端口：");
				string port3 = Term.ReadLine();
				new Thread(() =>
				{
					new LockServer(name2, port3);
				}).Start();
			}
			if (text == "searchserver")
			{
				Term.Write("服务器号：");
				string text65 = Term.ReadLine();
				Term.WriteLine(Http.x19post("https://g79mclobt.minecraft.cn", "/rental-server/query/search-by-name", "{\"server_name\": \"" + text65 + "\", \"offset\": 0}"));
			}
			if (text == "getserverip")
			{
				Term.Write("服务器号：");
				string text66 = Term.ReadLine();
				Term.Write("服务器密码：");
				string text67 = Term.ReadLine();
				Term.WriteLine(Http.x19post("https://g79mclobt.minecraft.cn", "/rental-server-world-enter/get", "{\"server_id\": \"" + text66 + "\", \"pwd\": \"" + text67 + "\"}"));
			}
			if (text == "lockserverv3" && !start_stop)
			{
				Term.Write("输入ip：");
				string ip4 = Term.ReadLine();
				Term.Write("输入端口：");
				string port4 = Term.ReadLine();
				Term.Write("输入服务器号：");
				string sid = Term.ReadLine();
				new LockServer(ip4, port4, sid);
			}
			if (text == "lockserverv4" && !start_stop)
			{
				Term.Write("输入ip：");
				string ip5 = Term.ReadLine();
				Term.Write("输入端口：");
				string port5 = Term.ReadLine();
				Term.Write("输入刷屏文件：");
				string spfile = Term.ReadLine();
				new LockServer.SPLock(ip5, port5, spfile);
			}
			if (text == "lockserverv6" && !start_stop)
			{
				try
				{
					Term.Write("输入服务器号：");
					string text68 = Term.ReadLine();
					new LockServer(JsonConvert.DeserializeObject<JObject>(Http.x19post("https://g79mclobt.minecraft.cn", "/rental-server/query/search-by-name", "{\"server_name\": \"" + text68 + "\", \"offset\": 0}"))["entities"][0]["entity_id"].ToString());
				}
				catch (Exception ex44)
				{
					Term.WriteLine(ex44.ToString());
				}
			}
			if (text == "lockserverv5" && !start_stop)
			{
				Term.Write("输入ip：");
				string ip6 = Term.ReadLine();
				Term.Write("输入端口：");
				string port6 = Term.ReadLine();
				new LockServer.SPLockLine(ip6, port6);
			}
			if (text == "setauthserver")
			{
				Term.Write("输入认证服：");
				LockServer.AuthServerUrl = Term.ReadLine();
			}
			if (text == "setwebserver")
			{
				Term.Write("输入认证服：");
				LockServer.WebServerUrl = Term.ReadLine();
			}
			if (text == "displaywindow")
			{
				Term.Write("y/n:");
				string text69 = Term.ReadLine();
				if ((text69 == "y") | (text69 == "Y"))
				{
					FLockServer.DisplayWindow = true;
				}
				else if ((text69 == "n") | (text69 == "N"))
				{
					FLockServer.DisplayWindow = false;
				}
				else
				{
					Term.WriteLine("输入错误");
				}
			}
			if (text == "getkey")
			{
				Term.Write("输入文件名：");
				string filename = Term.ReadLine();
				Term.Write("输入数量：");
				string s11 = Term.ReadLine();
				Term.Write("输入时间：");
				string s12 = Term.ReadLine();
				Term.Write("输入初始向量：");
				string s13 = Term.ReadLine();
				DummySrv.Key key5 = new DummySrv.Key();
				int num10 = int.Parse(s11);
				int num11 = int.Parse(s13);
				int.Parse(s12);
				string text70 = string.Empty;
				string empty = string.Empty;
				for (int num12 = 0; num12 < num10; num12++)
				{
					key5.time = uint.Parse(s12);
					key5.id = (uint)(num11 + num12);
					empty = JsonConvert.SerializeObject(key5);
					text70 = text70 + x19Crypt.HttpEncrypt_g79v12(Encoding.UTF8.GetBytes(empty)).ToHex() + "\n";
				}
				Function.SaveFile(filename, text70);
				Term.WriteLine("文件已保存");
			}
			if (text == "fastbjd" || text == "fastec")
			{
				// 这两条以前没有任何兜底：路径错了 File.WriteAllText 直接抛出去，
				// 命令线程一死，后面所有命令（包括再点一次）都变成「点了没反应」。
				bool isBjd = text == "fastbjd";
				string label = isBjd ? "一键布吉岛" : "一键EC";
				if (CheckStart(0))
				{
					try
					{
						// 不再起本地认证服：客户端直连 g79authobt.nie.netease.com（与联机大厅同一条路）。
						// 本地代理那条线（g79authobt.minecraft.cn/authentication-v2）现在稳定返回 400/空 body。
						Http.server_id = isBjd ? "4638866978460665031:NetworkGame" : "4619956744566574402:NetworkGame";
						string contents5 = isBjd ? Function.FastAUTHgame() : Function.FastAUTHec();
						File.WriteAllText(Temp.path + "\\mc.cfg", contents5);
						Term.WriteLine("✓ " + label + "：服务器 " + Http.server_id + "，认证走官方网关，正在启动客户端…");
						InvokeCmdAsync("start " + Temp.path + "\\Minecraft.Windows.exe");
					}
					catch (Exception ex)
					{
						Term.WriteLine("✗ " + label + " 启动失败：" + ex.Message);
					}
				}
			}
			if (text == "sproom" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("房间号：");
					string rid2 = Term.ReadLine();
					Term.Write("输入刷屏文件：");
					string name3 = Term.ReadLine();
					if (name3 == "")
					{
						name3 = "ChaCha.txt";
					}
					Term.Write("密码：");
					string pwd = Term.ReadLine();
					Random random3 = new Random();
					new Thread(() =>
					{
						try
						{
							start_stop = true;
							string text79 = DummySrv.strings[random3.Next(0, DummySrv.strings.Count)];
							DummySrv.RoomID.Add(int.Parse(rid2));
							new SpTask(ReadJson(text79), rid2, pwd, name3);
							DummySrv.RoomID.Remove(int.Parse(rid2));
							DummySrv.strings.Add(text79);
						}
						catch
						{
						}
					}).Start();
				}
				catch
				{
				}
			}
			if (text == "spserver" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				start_stop = true;
				try
				{
					Term.Write("输入ip：");
					string name4 = Term.ReadLine();
					Term.Write("输入端口：");
					string port7 = Term.ReadLine();
					Term.Write("输入服务器id：");
					string sid2 = Term.ReadLine();
					Term.Write("输入刷屏文件：");
					string file = Term.ReadLine();
					if (file == "")
					{
						file = "ChaCha.txt";
					}
					Random random4 = new Random();
					new Thread(() =>
					{
						try
						{
							start_stop = true;
							string text79 = DummySrv.strings[random4.Next(0, DummySrv.strings.Count)];
							new RentalSwipe(ReadJson(text79), sid2, name4, port7, file);
							DummySrv.strings.Add(text79);
						}
						catch
						{
						}
					}).Start();
				}
				catch
				{
				}
			}
			if (text == "asproom")
			{
				try
				{
					Term.Write("输入入房间entityid：");
					Term.ReadLine();
					Term.Write("输入刷屏文件：");
					string file2 = Term.ReadLine();
					if (file2 == "")
					{
						file2 = "ChaCha.txt";
					}
					new Random();
					new Thread(() =>
					{
						while (true)
						{
							JObject jObject9 = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("", "/online-lobby-game-enter"));
							if (jObject9["code"].ToString() == "52001")
							{
								break;
							}
							if (jObject9["code"].ToString() == "12031")
							{
								Thread.Sleep(1000);
							}
							else
							{
								string text79 = jObject9["entity"]["server_host"].ToString();
								string text80 = jObject9["entity"]["server_port"].ToString();
								InvokeCmd("start FastBuilder.exe --uid " + Http.UID + " --sid 4654415063569776560:LobbyGame --name " + displayName + " --version " + Temp.Version + " --engineVersion " + Temp.engineVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.nie.netease.com --ip " + text79 + " --port " + text80 + " --token " + CppGameM.GetLoginToken() + " --typ sp --spfile " + file2);
							}
						}
					}).Start();
				}
				catch
				{
				}
			}
			if (text == "setsleep")
			{
				try
				{
					Term.Write("输入间隔：");
					RentalSwipe.Sleep = int.Parse(Term.ReadLine());
				}
				catch
				{
				}
			}
			if (text == "ridlist" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				for (int num13 = 0; num13 < DummySrv.RoomID.Count; num13++)
				{
					if (DummySrv.RoomID.Count == 0)
					{
						break;
					}
					Term.WriteLine(DummySrv.RoomID[num13]);
				}
			}
			if (text == "padding" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				Term.Write("输入ip：");
				string ip7 = Term.ReadLine();
				Term.Write("输入端口：");
				string port8 = Term.ReadLine();
				Term.Write("服务器号：");
				Padding.StartPadding(Term.ReadLine(), ip7, port8);
			}
			if (text == "paddinglobby" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				Term.Write("输入ip：");
				string ip8 = Term.ReadLine();
				Term.Write("输入端口：");
				string port9 = Term.ReadLine();
				PaddingLobbyClient.StartPadding(ip8, port9);
			}
			if (text == "paddinglobbyt" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				Term.Write("输入ip：");
				string ip9 = Term.ReadLine();
				Term.Write("输入端口：");
				string port10 = Term.ReadLine();
				Term.Write("输入任务数量：");
				int num14 = int.Parse(Term.ReadLine());
				for (int num15 = 0; num15 < num14; num15++)
				{
					PaddingLobbyClient.StartPadding(ip9, port10);
					Thread.Sleep(100);
				}
			}
			if (text == "setnullpadding" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("输入预留空位：");
					Padding.NullPadding = int.Parse(Term.ReadLine());
				}
				catch
				{
				}
			}
			if (text == "tasknum")
			{
				Term.WriteLine(Padding.paddingClients.Count);
			}
			if (text == "setlockserverpassword")
			{
				Term.Write("设置密码：");
				LockServer.password = Term.ReadLine();
			}
			if (text == "displayentity")
			{
				Term.Write("y/n:");
				string text71 = Term.ReadLine();
				if ((text71 == "y") | (text71 == "Y"))
				{
					Padding.DisplayEntity = true;
				}
				else if ((text71 == "n") | (text71 == "N"))
				{
					Padding.DisplayEntity = false;
				}
				else
				{
					Term.WriteLine("输入错误");
				}
			}
			if (text == "lockUID" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					if (!start_stop)
					{
						Term.Write("输入uid：");
						TraceUIDLockServer.TraceUser(Term.ReadLine());
					}
				}
				catch (Exception ex45)
				{
					Term.WriteLine(ex45.ToString());
				}
			}
			if (text == "lockpwd" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("输入密码：");
					TraceUIDLockServer.LockPwd = Term.ReadLine();
				}
				catch (Exception ex46)
				{
					Term.WriteLine(ex46.ToString());
				}
			}
			if (text == "title")
			{
				Term.Write("输入标题：");
				Term.Title = Term.ReadLine();
			}
			if (text == "clear")
			{
				Term.Clear();
			}
			if (text == "cls")
			{
				Term.Clear();
			}
			if (text == "color")
			{
				try
				{
					Term.Write("输入色号：");
					switch (int.Parse(Term.ReadLine()))
					{
					case 0:
						Term.ForegroundColor = ConsoleColor.Black;
						break;
					case 1:
						Term.ForegroundColor = ConsoleColor.DarkBlue;
						break;
					case 2:
						Term.ForegroundColor = ConsoleColor.DarkGreen;
						break;
					case 3:
						Term.ForegroundColor = ConsoleColor.DarkCyan;
						break;
					case 4:
						Term.ForegroundColor = ConsoleColor.DarkRed;
						break;
					case 5:
						Term.ForegroundColor = ConsoleColor.DarkMagenta;
						break;
					case 6:
						Term.ForegroundColor = ConsoleColor.DarkYellow;
						break;
					case 7:
						Term.ForegroundColor = ConsoleColor.Gray;
						break;
					case 8:
						Term.ForegroundColor = ConsoleColor.DarkGray;
						break;
					case 9:
						Term.ForegroundColor = ConsoleColor.Blue;
						break;
					case 10:
						Term.ForegroundColor = ConsoleColor.Green;
						break;
					case 11:
						Term.ForegroundColor = ConsoleColor.Cyan;
						break;
					case 12:
						Term.ForegroundColor = ConsoleColor.Red;
						break;
					case 13:
						Term.ForegroundColor = ConsoleColor.Magenta;
						break;
					case 14:
						Term.ForegroundColor = ConsoleColor.Yellow;
						break;
					case 15:
						Term.ForegroundColor = ConsoleColor.White;
						break;
					}
				}
				catch
				{
				}
			}
			if (text == "reportv2" && (DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				try
				{
					Term.Write("输入uid：");
					string text72 = Term.ReadLine();
					Random random5 = new Random();
					AuthenticationResponse authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(Http.PE_GetLoginResponse(ReadJson(DummySrv.strings[random5.Next(0, DummySrv.strings.Count - 1)])));
					byte[] mD5token = HashHelper.ComputeHash(authenticationResponse.entity.token);
					string text73 = JsonConvert.DeserializeObject<JObject>(Http.x19post("https://g79apigatewayobt.minecraft.cn", "/user/query/search-by-uid", "{\"user_id\":\"" + text72 + "\"}", mD5token, authenticationResponse.entity.entity_id))["entity"]["name"].ToString();
					ReportEntity reportEntity = new ReportEntity();
					ReportEntities reportEntities = new ReportEntities();
					reportEntities.act_timestamp = Function.GetTimeStampUlong();
					reportEntities.reach_login_time = Function.GetTimeStampUlong();
					string[] array18 = new string[10] { "习近平已经死了，江泽民迫害法论功，法轮功大法好，国民党万岁，毛泽东不得好死！", "你妈骚臭阴道里的黑鸡鸡", "慰安妇", "木合塔尔", "人大代表撸管导管", "大鸡巴", "毒枭", "打飞机", "汉奸", "台湾" };
					string text74 = string.Empty;
					for (int num16 = 0; num16 < array18.Length; num16++)
					{
						text74 = text74 + text73 + ":" + array18[num16] + ";";
					}
					reportEntities.report_evi = text74;
					reportEntities.report_id = text72;
					reportEntities.report_name = text73;
					reportEntities.rep_reason = "恶意玩家,其他,发布不良信息,语言暴力,不雅昵称,外挂";
					reportEntity.type = "social_report_operation";
					reportEntity.data = reportEntities;
					string data = JsonConvert.SerializeObject(reportEntity);
					Http.x19post("https://g79apigatewayobt.minecraft.cn", "/salog", data, mD5token, authenticationResponse.entity.entity_id);
				}
				catch (Exception ex47)
				{
					Term.WriteLine(ex47.ToString());
				}
			}
			if (text == "report")
			{
				try
				{
					Term.Write("输入uid：");
					string text75 = Term.ReadLine();
					Term.Write("举报信息：");
					string text76 = Term.ReadLine();
					string text77 = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"user_id\":\"" + text75 + "\"}", "/user/query/search-by-uid"))["entity"]["name"].ToString();
					ReportEntity reportEntity2 = new ReportEntity();
					ReportEntities reportEntities2 = new ReportEntities();
					reportEntities2.act_timestamp = Function.GetTimeStampUlong();
					reportEntities2.reach_login_time = Function.GetTimeStampUlong();
					reportEntities2.report_evi = text77 + ":" + text76 + ";";
					reportEntities2.report_id = text75;
					reportEntities2.report_name = text77;
					ReportEntities data2 = reportEntities2;
					reportEntity2.type = "social_report_operation";
					reportEntity2.data = data2;
					string text78 = JsonConvert.SerializeObject(reportEntity2);
					Term.WriteLine(text78);
					Http.x19post("https://g79apigatewayobt.minecraft.cn", "/salog", text78);
				}
				catch (Exception ex48)
				{
					Term.WriteLine(ex48.ToString());
				}
			}
			if (!(text == "areport") || !(DummySrv.IsInit & (DummySrv.dummyUsers != null)))
			{
				continue;
			}
			try
			{
				Term.Write("输入uid：");
				string uid5 = Term.ReadLine();
				Random random6 = new Random();
				start_stop = true;
				new Thread(() =>
				{
					while (start_stop)
					{
						Term.WriteLine("sending a report");
						AuthenticationResponse authenticationResponse2 = JsonConvert.DeserializeObject<AuthenticationResponse>(Http.PE_GetLoginResponse(ReadJson(DummySrv.strings[random6.Next(0, DummySrv.strings.Count - 1)])));
						if (authenticationResponse2.code == 0)
						{
							byte[] mD5token2 = HashHelper.ComputeHash(authenticationResponse2.entity.token);
							string text79 = JsonConvert.DeserializeObject<JObject>(Http.x19post("https://g79apigatewayobt.minecraft.cn", "/user/query/search-by-uid", "{\"user_id\":\"" + uid5 + "\"}", mD5token2, authenticationResponse2.entity.entity_id))["entity"]["name"].ToString();
							ReportEntity reportEntity3 = new ReportEntity();
							ReportEntities reportEntities3 = new ReportEntities
							{
								act_timestamp = Function.GetTimeStampUlong(),
								reach_login_time = Function.GetTimeStampUlong()
							};
							string[] array19 = new string[10] { "习近平已经死了，江泽民迫害法论功，法轮功大法好，国民党万岁，毛泽东不得好死！", "你妈骚臭阴道里的黑鸡鸡", "慰安妇", "木合塔尔", "人大代表撸管导管", "大鸡巴", "毒枭", "打飞机", "汉奸", "台湾" };
							string text80 = string.Empty;
							for (int num17 = 0; num17 < array19.Length; num17++)
							{
								text80 = text80 + text79 + ":" + array19[num17] + ";";
							}
							reportEntities3.report_evi = text80;
							reportEntities3.report_id = uid5;
							reportEntities3.report_name = text79;
							reportEntities3.rep_reason = "恶意玩家,其他,发布不良信息,语言暴力,不雅昵称,外挂";
							reportEntity3.type = "social_report_operation";
							reportEntity3.data = reportEntities3;
							string data3 = JsonConvert.SerializeObject(reportEntity3);
							Http.x19post("https://g79apigatewayobt.minecraft.cn", "/salog", data3, mD5token2, authenticationResponse2.entity.entity_id);
							Term.WriteLine("report succeed");
						}
						else
						{
							Term.WriteLine(Regex.Unescape(authenticationResponse2.message));
						}
					}
				}).Start();
			}
			catch (Exception ex49)
			{
				Term.WriteLine(ex49.ToString());
			}
		}
	}

	public static string GetPlayerDisplayName(string uid)
	{
		try
		{
			return JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"user_id\":\"" + uid + "\"}", "/user/query/search-by-uid", displaylog: false))["entity"]["name"].ToString();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			return null;
		}
	}

	public static async void AKICK(string rid)
	{
		await Task.Run(() =>
		{
			try
			{
				while (start_stop)
				{
					Thread.Sleep(1000);
					string[] array = Temp.ban_id.ToArray();
					JObject jObject = JsonConvert.DeserializeObject<JObject>(Http.postAPIPC("{\"room_id\":\"" + rid + "\"}", "/online-lobby-member/query/list-by-room-id"));
					int num = jObject["entities"].Count();
					int num2 = 0;
					while (num2 < num)
					{
						for (int i = 0; i < array.Count(); i++)
						{
							JObject jObject2 = JsonConvert.DeserializeObject<JObject>(jObject["entities"][num - 1].ToString());
							if (jObject2["member_id"].ToString() == array[i])
							{
								Http.postAPIPC("{\"room_id\":\"" + rid + "\",\"user_id\":" + jObject2["member_id"].ToString() + "}", "/online-lobby-member-kick");
								Term.WriteLine("KickID:" + jObject2["member_id"].ToString());
								Term.WriteLine("玩家:" + jObject2["member_id"].ToString() + "尝试进入游戏已被踢出");
							}
						}
						num--;
					}
				}
			}
			catch (Exception ex)
			{
				Term.WriteLine("出错了：" + ex.Message);
			}
		});
	}

	public static async void Ajoin(string rid, string password = "")
	{
		await Task.Run(() =>
		{
			while (start_stop)
			{
				Http.postAPIPC("{\"room_id\":\"" + rid + "\",\"password\":\"" + password + "\",\"check_visibilily\":false}", "/online-lobby-room-enter");
				Http.room_id_public = rid;
				Thread.Sleep(1000);
			}
		});
	}

	public static void StaticAjoin(string rid, string password = "")
	{
		while (start_stop)
		{
			Http.postAPIPC("{\"room_id\":\"" + rid + "\",\"password\":\"" + password + "\",\"check_visibilily\":false}", "/online-lobby-room-enter");
			Thread.Sleep(1000);
		}
	}

	public static string ReadJson(string FilePath)
	{
		try
		{
			Function.ClientLog("[FILE] read path:" + FilePath);
			StreamReader streamReader = new StreamReader(FilePath);
			string result = streamReader.ReadToEnd();
			streamReader.Close();
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			Function.ClientError(ex.ToString());
			return null;
		}
	}

	public static string SimpleRead(string FilePath)
	{
		try
		{
			StreamReader streamReader = new StreamReader(FilePath);
			string result = streamReader.ReadToEnd();
			streamReader.Close();
			return result;
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			Function.ClientError(ex.ToString());
			return null;
		}
	}

	public static string GetAuthenticationOtpJson(string versionvaluas, string aid, string otp_token, string Sauth_json)
	{
		try
		{
			AuthoptSauth authoptSauth = new AuthoptSauth();
			VersionEntity versionEntity = new VersionEntity();
			AuthenticationEntity authenticationEntity = new AuthenticationEntity
			{
				sa_data = ReadJson("sa_data.json")
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

	/// <summary>
	/// 昵称输入钩子：图形界面里挂一个弹窗（SetNameDialog.Ask），
	/// 控制台 / --legacy 模式保持 null，仍走 Term.ReadLine。
	/// </summary>
	public static Func<string> NamePrompt;

	public static void Name()
	{
		int tries = 0;
		while (Function.GetName(player_info) == "")
		{
			if (++tries > 5)
			{
				Term.WriteLine("✗ 昵称多次设置失败，先跳过（稍后可用 setname 手动设置）");
				return;
			}

			Term.WriteLine("该账号还没有昵称!");
			string text;
			if (NamePrompt != null)
			{
				Term.WriteLine("[账号] 正在弹出昵称设置窗口…");
				text = NamePrompt();
				if (string.IsNullOrEmpty(text))
				{
					Term.WriteLine("已取消设置昵称（稍后可用 setname 手动设置）");
					return;
				}
			}
			else
			{
				Term.Write("输入你要设置的昵称：");
				text = Term.ReadLine();
			}

			if (text == "rj")
			{
				Http.SetName("NO" + Http.UID);
			}
			else
			{
				Http.SetName(text);
			}
			Http.GetName();
			if (Function.GetName(player_info) != "")
			{
				break;
			}
		}
	}

	public static string InvokeCmd(string cmdArgs)
	{
		Process process = new Process();
		process.StartInfo.FileName = "cmd.exe";
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardInput = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.Start();
		process.StandardInput.WriteLine(cmdArgs);
		process.StandardInput.WriteLine("exit");
		string result = process.StandardOutput.ReadToEnd();
		process.Close();
		return result;
	}

	public static int UnzipModJson(string path)
	{
		int num = sd.k(path);
		if (num != 0)
		{
			Term.WriteLine("UnzipModJson Failed! code:" + num);
			return num;
		}
		Term.WriteLine("code:0");
		return num;
	}

	public static async void InvokeCmdAsync(string cmdArgs)
	{
		await Task.Run(() =>
		{
			Process process = new Process();
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			process.StandardInput.WriteLine(cmdArgs);
			process.StandardInput.WriteLine("exit");
			process.StandardOutput.ReadToEnd();
			process.Close();
		});
	}

	public static async void updateroom(string room_id)
	{
		Http.postAPIPC("{\"room_id\":\"" + room_id + "\",\"password\":null,\"check_visibilily\":false}", "/online-lobby-member-tick");
		isroom = true;
		Term.WriteLine("已开启自动更新");
		await Task.Run(() =>
		{
			while (isroom)
			{
				Http.postAPIPC("{\"room_id\":\"" + room_id + "\",\"password\":null,\"check_visibilily\":false}", "/online-lobby-member-tick");
				Thread.Sleep(30000);
			}
		});
	}

	public static async void updateroom_NoCommon(string room_id, string token, string uid)
	{
		Http.postAPIPC_NoCommon("{\"room_id\":\"" + room_id + "\",\"password\":null,\"check_visibilily\":false}", "/online-lobby-member-tick", token, uid);
		isroom = true;
		Term.WriteLine("已开启自动更新");
		await Task.Run(() =>
		{
			while (isroom)
			{
				Http.postAPIPC_NoCommon("{\"room_id\":\"" + room_id + "\",\"password\":null,\"check_visibilily\":false}", "/online-lobby-member-tick", token, uid);
				Thread.Sleep(30000);
			}
		});
	}

	public static bool checkplayer(string[] str, string uid)
	{
		bool flag = true;
		for (int i = 0; i < str.Count(); i++)
		{
			if (str[i] == uid)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return true;
		}
		return false;
	}
}
