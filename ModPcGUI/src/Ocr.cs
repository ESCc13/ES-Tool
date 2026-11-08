using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using text;

internal class Ocr
{
	private static object inner_asyncObject = new object();

	private static int inner_startPort = 50001;

	public static string GetOcr(string imageBase64)
	{
		int unusedPort = GetUnusedPort();
		if (unusedPort == -1)
		{
			return null;
		}
		TcpListener tcpListener = new TcpListener(new IPEndPoint(Dns.GetHostAddresses("127.0.0.1")[0], unusedPort));
		tcpListener.Start();
		app.InvokeCmdAsync("OcrProxy.dll " + unusedPort);
		TcpClient tcpClient = tcpListener.AcceptTcpClient();
		NetworkStream stream = tcpClient.GetStream();
		BinaryReader binaryReader = new BinaryReader(stream);
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		byte[] bytes = Encoding.ASCII.GetBytes(imageBase64);
		bytes = BitConverter.GetBytes((uint)bytes.Length).Concat(bytes).ToArray();
		binaryWriter.Write(bytes);
		uint count = binaryReader.ReadUInt32();
		byte[] bytes2 = binaryReader.ReadBytes((int)count);
		tcpClient.Close();
		tcpListener.Stop();
		return Encoding.ASCII.GetString(bytes2);
	}

	public static int GetUnusedPort()
	{
		lock (inner_asyncObject)
		{
			List<int> portIsInOccupiedState = GetPortIsInOccupiedState();
			string portIsInOccupiedStateByNetStat = GetPortIsInOccupiedStateByNetStat();
			for (int i = inner_startPort; i < 60000; i++)
			{
				if (portIsInOccupiedStateByNetStat.IndexOf(":" + inner_startPort) < 0 && !portIsInOccupiedState.Contains(inner_startPort))
				{
					inner_startPort = i + 1;
					return i;
				}
			}
			return -1;
		}
	}

	private static List<int> GetPortIsInOccupiedState()
	{
		List<int> list = new List<int>();
		try
		{
			IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] activeTcpListeners = iPGlobalProperties.GetActiveTcpListeners();
			IPEndPoint[] activeUdpListeners = iPGlobalProperties.GetActiveUdpListeners();
			TcpConnectionInformation[] activeTcpConnections = iPGlobalProperties.GetActiveTcpConnections();
			list.AddRange(activeTcpListeners.Select((IPEndPoint m) => m.Port));
			list.AddRange(activeUdpListeners.Select((IPEndPoint m) => m.Port));
			list.AddRange(activeTcpConnections.Select((TcpConnectionInformation m) => m.LocalEndPoint.Port));
			list.Distinct();
			return list;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	private static string GetPortIsInOccupiedStateByNetStat()
	{
		string empty = string.Empty;
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo("netstat", "-an");
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.RedirectStandardOutput = true;
			process.Start();
			return process.StandardOutput.ReadToEnd().ToLower();
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}
}
