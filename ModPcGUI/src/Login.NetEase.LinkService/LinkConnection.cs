using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConsoleAppLogin.NetEase;
using MCStudio;
using MCStudio.Utils;
using Mcl.Core.Utils;
using Noya.LocalServer.Common.Cryptography;
using Noya.LocalServer.Common.Extensions;

namespace Login.NetEase.LinkService;

internal class LinkConnection
{
	[Serializable]
	public class Address
	{
		public string IP { get; set; }

		public int Port { get; set; }

		public int status { get; set; }

		public string ServerType { get; set; }

		public int id { get; set; }
	}

	public static bool Is_Connected;

	private byte[] _encrypt_key;

	private byte[] _decrypt_key;

	public ChaCha8 Encryption;

	public ChaCha8 Decryption;

	private string SERVER_PUBLIC_KEY_XML = "<RSAKeyValue><Exponent>AQAB</Exponent><Modulus>rht9ioo6tc3Z7On/80iYjNI+HpxnEpSc0tXC9JLykvwkxluZiLPrlvO6sgkkPsQMBXudGRu335dBCVdwfMefY7wswrQG51U+Nw3xfSSRgSptNV8PcmNjh6EYAluRSy7AZLcWc6+qJ6fJFOeABGYxNwMVvbpDC0R+t7BtcmQCk+4uXP2dsRjJSs6ALlfT7iEs8IL7iRfu1IvomTAc6eJarStgxEBTWdV/d2XfoIshbNYQ9ziBk0iWzHoI15UXFWLL+jZwhQYwzB0f+ilckgFT0IFKU4msUUQ7io4CBY2iI1G0BnSQYwpm84WR0HrgKL7uoxtJQ98iPk2GbZgWFv1OTw==</Modulus></RSAKeyValue>";

	private string CLIENT_PUBLIC_KEY_MD5 = "2ffdc15cb5e3790b92ff549f31390442";

	private string CLIENT_PRIVATE_KEY_XML = "<RSAKeyValue><Exponent>AQAB</Exponent><Modulus>zBHNrnH++2A7LQch+AOcJRYpTxo5f9rOlZOsWbbG+SYbfGxFUgatBdJ67vLQV4EQbUv++GyNlKMa79l+0kIJTG2FIveFgBpzLTBIvsNsJiVsWpmWZsFzUo0HMmd0JnZszJq/OqTm87PPpfj5RA8ydUDFq0YXIcZy4XZqHmXPfS2EcZ40OcTKHg5DRnxegHM5Avhq8rhSdUQzr7BVs2Im2Z83ePhk3lWxvhxLURtHq0A6BcAsR6cMCx2uKhnddbqPWmABsRAcvGfzKwdat3QXBsxRuTSbsXgznlM8AM52DC4TazGhesqwwsyKnhQZKqi0nLGuu9vgyX13ca2No0mu0w==</Modulus><D>m26QB+fB+7tfNzuwjtRJESJhEmP6Gb0SDnGtG6QQx2JUGx/oaMK29LFNe0SslYmzdlwk9xjPecAF21wAsasko/bjKi/3mgwLYAbf0ZTNgfyNHDDRkrCT4vOR4L1VhZo74leXgdZqJoL1jQgm68TbfN158atwIQSjKcFksISBVmiBGcQ8XVka6yg6D4Qob7DMHUCt8XFM5P50CQdqvUiq6oPWEoIy3nWVsrQDA9B+p0SXrDBFN0gqRIvc96wBbaAtgbHCVFGFMA+5xc+75ZZcN4de0aqFxOph2NjdcR3JdmZjCSw/6iEXIhV2dP3zIUulJA4geNiDL9SsD6Zw8WR7UQ==</D><P>8xLLsWCv+DZZjxYsjIxeCQjU8UZPqoRKYiGTctjcl0FK3yq4YGk0zoWUgqjKX10QXTmG6G5cF92DiAzLg5gnLm6P8euT0/JPgGZl9sVkvEecntig7HEiqs9yqWA3GDb150FqsPBBSIPANFZUZXVTk68qGVIKiG9rzCu3axV1bFs=</P><Q>1uwCMLOa2N5HZ2P6/qjTZQv0wjc19XzxA0S6A8UaW8vOPdc8hJB97FKMgOUkicxjXlMZSBO1sNT8Y1dPR15ufl/2QjcP88AliYFSO7nhhE8RojRLAZULzuC4hyEYYv8QUQy6TXE2Ta4AIi64OAcg20i/xij2cIKwBH9cwjSbMOk=</Q><DP>bpBUGsCyCiMepZkedme6tj1QLtcekZ9O/kfre8fsvtgyKESUTTZNkMrt/GiudKYuNVlfZgYc2bYmiBHZ2GezGsmrrAzN1xBW3T62joLHCWVBdndu612iuTNXInfjV55YR/JXh1ghOczD9op2JRgzBfAdJBtPMzQLQnl4GrtOCBU=</DP><DQ>t/5wmZUJWeRhqMfVVzLdV0J3FdYCYdnG04+A2D1jpXbDZ/neG3c/9pNtKeQB9d5+q3/kwunswCh2se1LN8RGP/aTcniFNZ4oBKIr7mniAU1XwU+XbxFUfJWyJC1XHVlTdK+6xxXG8ZWnE5x/paekn1aWp2TmJcgcPJ10oeY7fhE=</DQ><InverseQ>Uz+ZaNougfcTIXZcHyA5VFX28tSp640PeNy/5en3Pd9J5ocFel2VNlTBgy5AesON4uUK6bSehXL6d8ZK3KoxV5Bcr2zAttil07/KSg4gOfj0tqlsCVf044JmFuVBloFGLyj4fUQBA4YoGMHi2eDvh4bTZlhjeCRdnABlomhJTDE=</InverseQ></RSAKeyValue>";

	private string R1_KEY;

	private string R2_KEY;

	private byte[] Buffer = new byte[2048];

	private int m_cacheLen;

	private byte[] m_dataBuffer = new byte[2048];

	private Thread thread { get; set; }

	private Thread thread2 { get; set; }

	private Thread CloseConnect { get; set; }

	public Socket Socket { get; private set; }

	public LinkConnection()
	{
		try
		{
			Address address = GetAddress();
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(address.IP), address.Port);
			Function.ClientLog("Link Server IP:" + address.IP + " Port:" + address.Port);
			Socket.Connect(remoteEP);
			Is_Connected = true;
			R1_KEY = Function.GetRandomKey(32);
			Function.ClientLog("Link r1: " + R1_KEY);
			Socket.Send(RSAHelper.RSAEncrypt(SERVER_PUBLIC_KEY_XML, R1_KEY.HexToBytes()));
			Socket.Send(CLIENT_PUBLIC_KEY_MD5.HexToBytes());
			int count = Socket.Receive(Buffer);
			R2_KEY = RSAHelper.RSADecrypt(CLIENT_PRIVATE_KEY_XML, Buffer.Take(count).ToArray()).ToHex();
			Function.ClientLog("Link r2: " + R2_KEY);
			Encryption = new ChaCha8((R1_KEY + R2_KEY).HexToBytes());
			Decryption = new ChaCha8((R2_KEY + R1_KEY).HexToBytes());
			thread = new Thread(() =>
			{
				Tick();
			});
			thread.Start();
			StartRecvMessage();
			Do_Login();
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			PrivClose();
		}
	}

	private void PrivClose()
	{
		CloseConnect = new Thread(() =>
		{
			Close();
		});
		CloseConnect.Start();
	}

	private void StartRecvMessage()
	{
		thread2 = new Thread(() =>
		{
			Function.ClientLog("[LinkConnection] start Recv");
			recv();
		});
		thread2.Start();
	}

	public void Do_Login()
	{
		string randomKey = Function.GetRandomKey(32);
		string text = "{\"s2\":\"" + Noya.LocalServer.Common.Cryptography.AESHelper.AES_ECB_Encrypt(CppGameM.GetLoginTokenBytes(), randomKey.HexToBytes()).ToHex() + "\",\"s1\":\"" + randomKey + "\",\"is_zip\":false,\"uid\":" + uint.Parse(Http.UID) + "}";
		Function.ClientLog("LoginV2: " + text);
		sendMessage(0, "LoginV2", text);
	}

	public void sendMessage(int server_id, string method, string data)
	{
		byte[] message = VarInt(server_id).Concat(new byte[1]).Concat(new byte[1] { (byte)method.Length }).Concat(Encoding.UTF8.GetBytes(method))
			.Concat(Encoding.UTF8.GetBytes(data))
			.ToArray();
		SendMsg(message);
	}

	public void SendMsg(byte[] message)
	{
		Encryption.Process(message);
		Socket.Send(StructUtil.VarInt(message.Length).Concat(message).ToArray());
	}

	private Address GetAddress()
	{
		Function.ClientLog("Link Server Initialize");
		string chatServerList = GetChatServerList();
		List<Address> source = JsonHelper.DeserializeObject<List<Address>>((chatServerList != null) ? chatServerList : null);
		source = source.Where((Address x) => x.status == 0).ToList();
		if (source == null || source.Count == 0)
		{
			return null;
		}
		int index = NumberHelper.RandomNumber(0, source.Count);
		return source[index];
	}

	private string GetChatServerList()
	{
		Function.ClientLog("Link Server HTTP/1.1 linkserver_obt.list");
		return new StreamReader(((HttpWebResponse)((HttpWebRequest)WebRequest.Create("https://g79.update.netease.com/linkserver_obt.list")).GetResponse()).GetResponseStream(), Encoding.UTF8).ReadToEnd();
	}

	public byte[] VarInt(int value)
	{
		List<byte> list = new List<byte>();
		while (value >= 128)
		{
			byte item = (byte)((value & 0xFF) | 0x80);
			list.Add(item);
			value >>= 7;
		}
		list.Add((byte)value);
		return list.ToArray();
	}

	public static int BytesToVarInt(byte[] buf, out int size)
	{
		int num = 0;
		int num2 = 0;
		for (size = 0; size < buf.Length; size++)
		{
			byte b = buf[size];
			if (b < 128)
			{
				size++;
				return num | (b << num2);
			}
			num |= b & (127 << num2);
			num2 += 7;
		}
		return 0;
	}

	public void recv()
	{
		while (true)
		{
			try
			{
				int num = Socket.Receive(Buffer);
				Array.Copy(Buffer.Take(num).ToArray(), 0, m_dataBuffer, m_cacheLen, num);
				m_cacheLen += num;
				int i;
				int num2;
				int size;
				for (i = 0; num >= i + 2; i += num2 + size)
				{
					num2 = BytesToVarInt(m_dataBuffer.Skip(i).ToArray(), out size);
					if (num < i + num2 + size)
					{
						break;
					}
					HandleMessage(m_dataBuffer, i + size, num2);
				}
				m_cacheLen -= i;
				Array.Copy(m_dataBuffer, i, m_dataBuffer, 0, m_cacheLen);
			}
			catch (Exception ex)
			{
				Term.WriteLine(ex.ToString());
				PrivClose();
				break;
			}
		}
	}

	private void HandleMessage(byte[] data, int start, int len)
	{
		byte[] array = new byte[len];
		Array.Copy(data, start, array, 0, len);
		Decryption.Process(array);
		Function.ClientLog("[LinkConnection]" + Encoding.UTF8.GetString(array));
	}

	private void Tick()
	{
		byte[] buffer = new byte[2];
		while (true)
		{
			try
			{
				Thread.Sleep(30000);
				Socket.Send(buffer);
			}
			catch (Exception ex)
			{
				Term.WriteLine(ex.ToString());
				PrivClose();
				break;
			}
		}
	}

	public void Close()
	{
		try
		{
			Socket.Close();
			Is_Connected = false;
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
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
			Is_Connected = false;
		}
	}
}
