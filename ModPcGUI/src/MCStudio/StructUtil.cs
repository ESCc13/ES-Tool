using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCStudio;

public class StructUtil
{
	public static int BytesToVarInt(byte[] buf, out int headerSize)
	{
		int num = 0;
		int num2 = 0;
		for (headerSize = 0; headerSize < buf.Length; headerSize++)
		{
			byte b = buf[headerSize];
			if (b < 128)
			{
				headerSize++;
				return num | (b << num2);
			}
			num |= b & (127 << num2);
			num2 += 7;
		}
		return 0;
	}

	public static byte[] VarInt(int value)
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

	public static byte[] GetRandomBytes(int length)
	{
		byte[] array = new byte[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = (byte)new Random().Next(0, 256);
		}
		return array;
	}

	public static byte[] PackMessage(int server_id, string method, string data)
	{
		return VarInt(server_id).Concat(new byte[1]).Concat(new byte[1] { (byte)method.Length }).Concat(Encoding.UTF8.GetBytes(method))
			.Concat(Encoding.UTF8.GetBytes(data))
			.ToArray();
	}
}
