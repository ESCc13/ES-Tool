using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using MCStudio.Utils;
using Mcl.Core.Utils;

namespace MCStudio.Network.GameControl;

public class RPC
{
	public class ConnectMsg
	{
		public short id;

		public ushort tokenLen;

		public string tokenStr;
	}

	public int FingerPrint;

	private ICommunicable m_comm;

	private bool m_isLogin;

	private ChaCha8 m_RecvKey;

	private ChaCha8 m_SendKey;

	public static int StaticControlPort = 10097;

	private int m_launchIdx = -1;

	private bool m_isLaunchIdxReady;

	private SocketCallback m_socketCallbackFuc = new SocketCallback();

	private TcpListener m_mcControlListener;

	private const string m_launcherIp = "127.0.0.1";

	private bool m_isNormalExit;

	public Dictionary<string, Action> CloseActions = new Dictionary<string, Action>();

	public Dictionary<string, Action> ReadyActions = new Dictionary<string, Action>();

	private bool m_isGame;

	private List<byte[]> _sendCache = new List<byte[]>();

	public TcpClient Client { get; private set; }

	public BinaryReader Reader { get; private set; }

	public BinaryWriter Writer { get; private set; }

	public int LauncherControlPort { get; protected set; }

	public RPC(ICommunicable comm, bool game = true)
	{
		m_comm = comm;
		m_isGame = game;
		LauncherControlPort = GetUsablePort();
		if (PortInUse(LauncherControlPort))
		{
			LauncherControlPort = GetUsablePort();
		}
		StartControlConnection(2);
		if (game)
		{
			m_socketCallbackFuc.RegisterReceiveCallBack(0, OnConnect);
		}
	}

	private void OnConnect(byte[] content)
	{
		ConnectMsg content2 = new ConnectMsg();
		new SimpleUnpack(content).Unpack(ref content2);
		byte[] bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(content2.tokenStr);
		byte[] array = new byte[32];
		Array.Clear(array, 0, array.Length);
		Array.Copy(bytes, 16, array, 0, 16);
		Array.Copy(bytes, 0, array, 16, 16);
		m_RecvKey = new ChaCha8(array);
		m_SendKey = new ChaCha8(bytes);
		m_isLogin = true;
	}

	private void StartControlConnection(int trytimes = 0)
	{
		try
		{
			m_mcControlListener = new TcpListener(IPAddress.Parse("127.0.0.1"), LauncherControlPort);
			m_mcControlListener.Start();
		}
		catch (Exception err)
		{
			if (trytimes > 0)
			{
				LauncherControlPort = GetUsablePort();
				StartControlConnection(trytimes - 1);
			}
			else
			{
				Logger.Default.Error(err, "StartControlConnection");
				CloseControlConnection();
			}
			return;
		}
		_ = m_isGame;
	}

	public void RegisterReceiveCallBack(ushort sid, Action<byte[]> callback)
	{
		m_socketCallbackFuc.RegisterReceiveCallBack(sid, callback);
	}

	private void ListenControlConnect()
	{
		try
		{
			if (m_mcControlListener == null)
			{
				Logger.Default.Error("[ListenControlConnect] m_mcControlListener is null");
				return;
			}
			while (true)
			{
				TcpClient tcpClient = m_mcControlListener.AcceptTcpClient();
				if (tcpClient.Client.RemoteEndPoint.ToString().Split(':')[0] != "127.0.0.1")
				{
					tcpClient.Close();
					continue;
				}
				NetworkStream stream = tcpClient.GetStream();
				Client = tcpClient;
				Reader = new BinaryReader(stream);
				Writer = new BinaryWriter(stream);
				SendCacheControlData();
			}
		}
		catch
		{
			CloseControlConnection();
		}
	}

	private void OnRecvControlData()
	{
		while (!m_isNormalExit)
		{
			byte[] message;
			try
			{
				int count = Reader.ReadUInt16();
				message = Reader.ReadBytes(count);
			}
			catch (Exception ex)
			{
				if (!(ex is EndOfStreamException) && !(ex is IOException))
				{
					Logger.Default.Error(ex, "OnRecvControlData");
				}
				if (!m_isNormalExit)
				{
					CloseGameCleaning();
				}
				break;
			}
			if (m_isGame)
			{
				HandleMcControlMessage(message);
			}
			else
			{
				HandleEditorControlMessage(message);
			}
		}
	}

	private void HandleMcControlMessage(byte[] message)
	{
		if (m_isLogin)
		{
			m_RecvKey.Process(message);
		}
		ushort num = BitConverter.ToUInt16(message, 0);
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

	private void SendCacheControlData()
	{
		foreach (byte[] item in _sendCache)
		{
			SendControlData(item);
		}
		_sendCache.Clear();
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
		catch (Exception err)
		{
			Logger.Default.Error(err, "SendControlData");
		}
	}

	public void CloseControlConnection()
	{
		try
		{
			m_mcControlListener?.Stop();
			m_mcControlListener = null;
		}
		catch (Exception err)
		{
			Logger.Default.Error(err, "CloseControlConnection");
		}
	}

	public void RegisterReadyAction(string name, Action action)
	{
		ReadyActions.Add(name, action);
	}

	public void RegisterCloseAction(string name, Action action)
	{
		CloseActions.Add(name, action);
	}

	private void ClearActions()
	{
		ReadyActions.Clear();
		CloseActions.Clear();
	}

	public void CloseGameCleaning()
	{
		try
		{
			ExecuteCloseActions();
			ClearActions();
		}
		catch (Exception)
		{
		}
		m_isLaunchIdxReady = false;
		m_launchIdx = -1;
	}

	private void ExecuteCloseActions()
	{
		foreach (KeyValuePair<string, Action> closeAction in CloseActions)
		{
			closeAction.Value();
		}
	}

	private void ExecuteReadyActions()
	{
		foreach (KeyValuePair<string, Action> readyAction in ReadyActions)
		{
			readyAction.Value();
		}
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

	public void NormalExit()
	{
		m_isNormalExit = true;
		LauncherControlPort = 0;
		Close();
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
}
