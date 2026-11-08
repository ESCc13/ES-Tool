using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Login.NetEase.DummyService;
using Login.NeteaseEntity;
using MCStudio.Utils;
using Mcl.Core.Utils;
using Newtonsoft.Json;
using Noya.LocalServer.Common.Cryptography;
using Noya.LocalServer.Common.Extensions;
using text;

namespace Login.NetEase.ChatService;

internal class ChatConnection
{
	[Serializable]
	public class Address
	{
		public string IP { get; set; }

		public int Port { get; set; }

		public int status { get; set; } = 1;
	}

	public static bool Is_Connected;

	public NetworkStream networkStream;

	private TcpClient tcpClient;

	private byte[] _encrypt_key = new byte[32];

	private byte[] _decrypt_key = new byte[32];

	public ChaCha8 Encryption;

	public ChaCha8 Decryption;

	private string ChaCha_KEY;

	private Thread thread { get; set; }

	private Thread thread2 { get; set; }

	private Thread CloseConnect { get; set; }

	public BinaryReader Reader { get; private set; }

	public BinaryWriter Writer { get; private set; }

	public ChatConnection(bool tick = true)
	{
		try
		{
			Address address = GetAddress();
			byte[] array = new byte[42];
			tcpClient = new TcpClient();
			tcpClient.Connect(IPAddress.Parse(address.IP), address.Port);
			networkStream = tcpClient.GetStream();
			Is_Connected = true;
			Reader = new BinaryReader(networkStream);
			Writer = new BinaryWriter(networkStream);
			ChaCha_KEY = Function.GetRandomKey(32);
			Function.ClientLog("[ConnectLog] ChaChaKey:" + ChaCha_KEY);
			byte[] sourceArray = new byte[6] { 40, 0, 1, 0, 1, 1 };
			byte[] array2 = ChaCha_KEY.HexToBytes();
			Array.Copy(sourceArray, 0, array, 0, 6);
			Array.Copy(array2, 0, array, 6, 16);
			byte[] bytes = Encoding.ASCII.GetBytes(Http.LoginSRCToken);
			byte[] sourceArray2 = Noya.LocalServer.Common.Cryptography.AESHelper.AES_ECB_Encrypt(bytes, array2);
			byte[] bytes2 = BitConverter.GetBytes(uint.Parse(Http.UID));
			Array.Copy(sourceArray2, 0, array, 22, 16);
			Array.Copy(bytes2, 0, array, 38, 4);
			networkStream.Write(array, 0, array.Length);
			Writer.Flush();
			byte[] sourceArray3 = ChaCha_KEY.HexToBytes();
			Array.Copy(bytes, 0, _encrypt_key, 0, 16);
			Array.Copy(sourceArray3, 0, _encrypt_key, 16, 16);
			Array.Copy(sourceArray3, 0, _decrypt_key, 0, 16);
			Array.Copy(bytes, 0, _decrypt_key, 16, 16);
			Encryption = new ChaCha8(_encrypt_key);
			Decryption = new ChaCha8(_decrypt_key);
			if (tick)
			{
				thread = new Thread(() =>
				{
					Tick();
				});
				thread.Start();
				thread2 = new Thread(() =>
				{
					StartRecvMessage();
				});
				thread2.Start();
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			Close();
		}
	}

	private void StartRecvMessage()
	{
		recv();
	}

	private void PrivClose()
	{
		CloseConnect = new Thread(() =>
		{
			Close();
		});
		CloseConnect.Start();
	}

	public void Close()
	{
		try
		{
			try
			{
				thread.Abort();
			}
			catch
			{
			}
			try
			{
				thread2.Abort();
			}
			catch
			{
			}
			Writer.Flush();
			networkStream.Flush();
			Reader.Close();
			Writer.Close();
			networkStream.Close();
			tcpClient.Close();
			Is_Connected = false;
		}
		catch
		{
		}
	}

	public void recv()
	{
		try
		{
			Reader.ReadBytes(7);
			while (true)
			{
				int num = Reader.ReadUInt16();
				Reader.ReadUInt16();
				byte[] array = Reader.ReadBytes(num - 2);
				Writer.Flush();
				if (array == null)
				{
					continue;
				}
				byte[] array2 = array;
				Decryption.Process(array2);
				byte[] array3 = new byte[2];
				Array.Copy(array2, 0, array3, 0, 2);
				uint num2 = BitConverter.ToUInt16(array3, 0);
				Function.ClientLog("[ConnectLog] code:" + num2);
				byte[] array4 = new byte[array2.Length - 2];
				Array.Copy(array2, 2, array4, 0, array2.Length - 2);
				string text = Encoding.UTF8.GetString(array4);
				Function.ClientLog("[ConnectLog] Message:" + text);
				if (num2 == 10242)
				{
					ChatRoomInfo chatRoomInfo = JsonConvert.DeserializeObject<ChatRoomInfo>(Encoding.UTF8.GetString(array4));
					string text2 = chatRoomInfo.uid.ToString();
					string[] array5 = app.Temp.ban_id.ToArray();
					if (chatRoomInfo.op == "in")
					{
						for (int i = 0; i < array5.Length; i++)
						{
							if (text2 == array5[i])
							{
								Http.postAPIPC("{\"room_id\":\"" + Http.room_id_public + "\",\"user_id\":" + text2 + "}", "/online-lobby-member-kick");
								Term.WriteLine("玩家:" + text2 + "尝试进入游戏已被踢出");
							}
						}
					}
				}
				if (DummySrv.IsInit)
				{
					if (num2 == 1281 && (!text.StartsWith("{\"player_chatver_id\":") & !text.StartsWith("{\"err\":4")))
					{
						DummySrv.Dummy(text);
					}
					if (num2 == 10001)
					{
						DummySrv.RecvFriend(text);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			PrivClose();
		}
	}

	private void Tick()
	{
		try
		{
			byte[] array = new byte[2];
			while (true)
			{
				networkStream.Write(array, 0, array.Length);
				Writer.Flush();
				Thread.Sleep(30000);
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			PrivClose();
		}
	}

	private Address GetAddress()
	{
		string chatServerList = GetChatServerList();
		List<Address> source = JsonHelper.DeserializeObject<List<Address>>((chatServerList != null) ? chatServerList : null);
		source = source.Where((Address x) => x.status == 1).ToList();
		if (source == null || source.Count == 0)
		{
			return null;
		}
		int index = NumberHelper.RandomNumber(0, source.Count);
		return source[index];
	}

	private string GetChatServerList()
	{
		return new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://x19.update.netease.com/chatserver.list")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd();
	}

	public void SendUIDMessage(string message, string uid)
	{
		try
		{
			string text = "[" + uid + ",\"" + message + "\"]";
			Term.WriteLine(text);
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			byte[] array = new byte[bytes.Length + 2];
			Array.Copy(BitConverter.GetBytes((ushort)1281), 0, array, 0, 2);
			Array.Copy(bytes, 0, array, 2, bytes.Length);
			app.chatConnection.Encryption.Process(array);
			byte[] array2 = new byte[array.Length + 2];
			Array.Copy(BitConverter.GetBytes((ushort)array.Length), 0, array2, 0, 2);
			Array.Copy(array, 0, array2, 2, array.Length);
			byte[] array3 = new byte[array2.Length + 2];
			Array.Copy(BitConverter.GetBytes((ushort)array2.Length), 0, array3, 0, 2);
			Array.Copy(array2, 0, array3, 2, array2.Length);
			app.chatConnection.networkStream.Write(array3, 0, array3.Length);
			Writer.Flush();
		}
		catch
		{
		}
	}
}
