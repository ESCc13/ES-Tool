using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Login;
using Login.AFunction;
using Login.NetEase;
using MCStudio.Network.Http;
using Newtonsoft.Json;

namespace text;

internal class Program
{
	public static NetworkStream NetworkStream;

	public static BinaryReader Reader;

	public static BinaryWriter Writer;

	public static TcpClient Client;

	public static string room_id = string.Empty;

	public static string password = string.Empty;

	public static string left_room = string.Empty;

	public static bool is_roomupdate = false;

	public static void LegacyMain(string[] args)
	{
		Term.Title = "Login";
		string fpd;
		if (args.Length == 0)
		{
			CoreNative.CanLaunch("", "", out fpd);
			app.Temp = JsonConvert.DeserializeObject<temp>(app.SimpleRead("config.json"));
			app.AppRun();
			return;
		}
		if (args[0] == "--LauncherPort")
		{
			Client = new TcpClient();
			Client.Connect("127.0.0.1", int.Parse(args[1]));
			NetworkStream = Client.GetStream();
			Reader = new BinaryReader(NetworkStream);
			Writer = new BinaryWriter(NetworkStream);
			new Thread(() =>
			{
				while (true)
				{
					try
					{
						Reader.ReadBytes(1);
					}
					catch
					{
						Environment.Exit(0);
					}
				}
			}).Start();
			app.Temp = JsonConvert.DeserializeObject<temp>(app.SimpleRead("config.json"));
			CoreNative.CanLaunch("", "", out fpd);
			Function.ClientLog("[LoadLibary] Initialize mcl.common.dll");
			app.AppRun();
			return;
		}
		CoreNative.CanLaunch("", "", out fpd);
		app.Temp = JsonConvert.DeserializeObject<temp>(app.SimpleRead("config.json"));
		if (args[0].StartsWith("config="))
		{
			Aapp aapp = new Aapp();
			string text = args[0];
			aapp.Arun(text.Substring(7));
		}
		if (args[0].StartsWith("ridconfig="))
		{
			Aapp aapp2 = new Aapp();
			string text2 = args[0];
			aapp2.Arun_rid(text2.Substring(10));
		}
		if (args[0].StartsWith("startserver="))
		{
			Aapp aapp3 = new Aapp();
			string text3 = args[0];
			aapp3.ServerRun(text3.Substring(12));
		}
		if (args[0].StartsWith("tokenpath="))
		{
			app.AppRun_(args[0].Substring(10));
		}
		if (args[0].StartsWith("--loadsauth"))
		{
			app.AppLoad(app.ReadJson(args[1]));
		}
		if (args[0].StartsWith("aupdate="))
		{
			Aapp aapp4 = new Aapp();
			string text4 = args[0];
			aapp4.ArunUpdate(text4.Substring(8));
		}
		if (args[0].StartsWith("uid="))
		{
			Aapp aapp5 = new Aapp();
			string text5 = args[0];
			aapp5.SendUID(text5.Substring(4), args[3], args[1], int.Parse(args[2]));
		}
		if (args[0].StartsWith("dummy="))
		{
			Aapp aapp6 = new Aapp();
			string text6 = args[0];
			aapp6.Dummy(text6.Substring(6), args[1], args[2], args[3]);
		}
	}
}
