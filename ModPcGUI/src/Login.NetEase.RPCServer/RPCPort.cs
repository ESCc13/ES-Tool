using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MCStudio.Network;
using MCStudio.Network.GameControl;
using MCStudio.Utils;
using Mcl.Core.Utils;
using Noya.LocalServer.Common.Extensions;
using text;

namespace Login.NetEase.RPCServer;

internal class RPCPort
{
	private bool m_isGame;

	private bool m_isNormalExit;

	private SocketCallback m_socketCallbackFuc = new SocketCallback();

	private ChaCha8 m_RecvKey;

	private ChaCha8 m_SendKey;

	public Dictionary<string, Action> ReadyActions = new Dictionary<string, Action>();

	private List<byte[]> _sendCache = new List<byte[]>();

	private bool m_isLogin;

	private bool m_isLaunchIdxReady;

	private int m_launchIdx = -1;

	private bool m_isConnect { get; set; }

	private TcpListener GameControlListener { get; set; }

	public TcpClient Client { get; private set; }

	public BinaryReader Reader { get; private set; }

	public BinaryWriter Writer { get; private set; }

	public int LauncherControlPort { get; set; }

	public RPCPort(int Port = 0, bool game = false, int lowport = 9000, int maxport = 13000)
	{
		if (Port == 0)
		{
			m_isGame = game;
			LauncherControlPort = RPC.GetUsablePort();
			for (int i = 0; i < 10; i++)
			{
				if (PortInUse(LauncherControlPort))
				{
					break;
				}
				LauncherControlPort = RPC.GetUsablePort();
			}
		}
		else
		{
			m_isGame = game;
			LauncherControlPort = Port;
		}
		StartControlConnection();
		if (game)
		{
			m_socketCallbackFuc.RegisterReceiveCallBack(0, OnConnect);
		}
	}

	public void startChaChaEnc()
	{
		if (m_isGame)
		{
			m_socketCallbackFuc.RegisterReceiveCallBack(0, OnConnect);
			m_isLogin = false;
			m_isLaunchIdxReady = false;
			Term.WriteLine("[INFO][ChaChaX] reset");
		}
		else
		{
			Term.WriteLine("[ERROR][ChaCha] the type is bedrock");
		}
	}

	public void StartControlConnection()
	{
		GameControlListener = new TcpListener(Dns.GetHostAddresses("127.0.0.1")[0], LauncherControlPort);
		GameControlListener.Start();
		new Task(ListenControlConnect).Start();
	}

	public void CloseControlConnection()
	{
		try
		{
			GameControlListener?.Stop();
			GameControlListener = null;
		}
		catch (Exception err)
		{
			Logger.Default.Error(err, "CloseControlConnection");
		}
	}

	public void ListenControlConnect()
	{
		while (CheckConnect())
		{
			try
			{
				Client = GameControlListener.AcceptTcpClient();
				NetworkStream stream = Client.GetStream();
				Reader = new BinaryReader(stream);
				Writer = new BinaryWriter(stream);
				while (CheckConnect())
				{
					try
					{
						int count = Reader.ReadUInt16();
						byte[] array = Reader.ReadBytes(count);
						if (app.Temp.connect_log)
						{
							Term.WriteLine("[INFO][CONNECT]" + array.ToHex());
						}
						Commands(array);
						if (m_isGame)
						{
							HandleMcControlMessage(array);
						}
						else
						{
							HandleEditorControlMessage(array);
						}
					}
					catch
					{
						break;
					}
				}
			}
			catch
			{
				break;
			}
		}
	}

	public void SendControlDataSRC(byte[] message)
	{
		if (Writer == null)
		{
			return;
		}
		byte[] bytes = BitConverter.GetBytes((ushort)message.Length);
		bytes = bytes.Concat(message).ToArray();
		try
		{
			Writer.Write(bytes);
			Writer.Flush();
		}
		catch (Exception)
		{
		}
	}

	public void SendControlData(byte[] message)
	{
		if (Writer == null)
		{
			_sendCache.Add(message);
			return;
		}
		if (m_isLogin)
		{
			m_SendKey.Process(message);
		}
		byte[] bytes = BitConverter.GetBytes((ushort)message.Length);
		bytes = bytes.Concat(message).ToArray();
		try
		{
			Writer.Write(bytes);
			Writer.Flush();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
		}
	}

	public static int GetUsablePort(int checkTimes = 0, int low = 9000, int high = 13000)
	{
		Random random = new Random();
		int num;
		while (true)
		{
			num = random.Next(low, high);
			if (PortInUse(num))
			{
				continue;
			}
			if (checkTimes <= 0)
			{
				break;
			}
			TcpClient tcpClient = new TcpClient();
			try
			{
				tcpClient.Connect("127.0.0.1", num);
				if (tcpClient.Connected)
				{
					return GetUsablePort(checkTimes - 1);
				}
				tcpClient.Close();
			}
			catch (Exception)
			{
				tcpClient.Close();
				return num;
			}
		}
		return num;
	}

	private static bool PortInUse(int port)
	{
		IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
		IPEndPoint[] activeTcpListeners = iPGlobalProperties.GetActiveTcpListeners();
		for (int i = 0; i < activeTcpListeners.Length; i++)
		{
			if (activeTcpListeners[i].Port == port)
			{
				return true;
			}
		}
		TcpConnectionInformation[] activeTcpConnections = iPGlobalProperties.GetActiveTcpConnections();
		for (int j = 0; j < activeTcpConnections.Length; j++)
		{
			if (activeTcpConnections[j].LocalEndPoint.Port == port)
			{
				return true;
			}
		}
		activeTcpListeners = iPGlobalProperties.GetActiveUdpListeners();
		for (int k = 0; k < activeTcpListeners.Length; k++)
		{
			if (activeTcpListeners[k].Port == port)
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckConnect()
	{
		try
		{
			if (GameControlListener != null)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private void OnConnect(byte[] content)
	{
		RPC.ConnectMsg content2 = new RPC.ConnectMsg();
		new SimpleUnpack(content).Unpack(ref content2);
		byte[] bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(content2.tokenStr);
		Term.WriteLine("[INFO][CONNECT] ID:" + content2.id);
		Term.WriteLine("[INFO][CONNECT] TokenLength:" + content2.tokenLen);
		Term.WriteLine("[INFO][CONNECT] Token:" + Convert.ToBase64String(bytes));
		byte[] array = bytes;
		byte[] array2 = new byte[32];
		Array.Clear(array2, 0, array2.Length);
		Array.Copy(array, 16, array2, 0, 16);
		Array.Copy(array, 0, array2, 16, 16);
		m_RecvKey = new ChaCha8(array2);
		m_SendKey = new ChaCha8(array);
		m_isLogin = true;
		Term.WriteLine("[INFO][CONNECT] initialize ChaChaX");
	}

	public void NormalExit()
	{
		m_isNormalExit = true;
		LauncherControlPort = 0;
		Close();
	}

	private void Close()
	{
		Reader?.Close();
		Writer?.Close();
		Client?.Close();
		m_isLogin = false;
		_sendCache.Clear();
		CloseControlConnection();
	}

	private void HandleMcControlMessage(byte[] message)
	{
		if (m_isLogin)
		{
			m_RecvKey.Process(message);
		}
		ushort num = BitConverter.ToUInt16(message, 0);
		Term.WriteLine("[INFO][CONNECT]code:" + num);
		byte[] paramlist = message.Skip(2).Take(message.Length - 2).ToArray();
		if (!m_isLaunchIdxReady && num == 261)
		{
			m_launchIdx = BitConverter.ToInt16(message, 2);
			m_isLaunchIdxReady = true;
			ExecuteReadyActions();
		}
		try
		{
			m_socketCallbackFuc.CallBack(num, paramlist);
		}
		catch (Exception)
		{
		}
	}

	private void HandleEditorControlMessage(byte[] message)
	{
		if (m_isLogin)
		{
			m_RecvKey.Process(message);
		}
		ushort num = BitConverter.ToUInt16(message, 0);
		Term.WriteLine("[INFO][CONNECT]code:" + num);
		byte[] array = message.Skip(2).Take(message.Length - 2).ToArray();
		try
		{
			if (!m_socketCallbackFuc.CallBack(num, array))
			{
				Encoding.UTF8.GetString(array);
				byte[] message2 = SimplePack.Pack(num, array);
				SendControlData(message2);
			}
		}
		catch (Exception)
		{
		}
	}

	private void ExecuteReadyActions()
	{
		foreach (KeyValuePair<string, Action> readyAction in ReadyActions)
		{
			readyAction.Value();
		}
	}

	public void RegisterReceiveCallBack(ushort sid, Action<byte[]> callback)
	{
		m_socketCallbackFuc.RegisterReceiveCallBack(sid, callback);
	}

	public void Commands(byte[] array)
	{
		ushort num = BitConverter.ToUInt16(array, 0);
		if (num == 2065)
		{
			Term.WriteLine("连接服务器成功！房间号：" + ByteArrayExtensions.bytesToInt(array.Skip(2).ToArray(), 0));
		}
		if (num == 273)
		{
			uint num2 = BitConverter.ToUInt32(array.Skip(2).ToArray(), 0);
			Term.WriteLine("有人进入了房间：" + Function.GetOwnerName(num2.ToString()) + "  UID:" + num2);
		}
		if (num == 529)
		{
			uint num3 = BitConverter.ToUInt32(array.Skip(2).ToArray(), 0);
			Term.WriteLine("有人退出了房间：" + Function.GetOwnerName(num3.ToString()) + "  UID:" + num3);
		}
	}
}
