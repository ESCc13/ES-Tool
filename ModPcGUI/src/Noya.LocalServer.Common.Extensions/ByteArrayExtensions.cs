using System;
using System.Text;

namespace Noya.LocalServer.Common.Extensions;

public static class ByteArrayExtensions
{
	public static byte[] Xor(this byte[] buffer, byte @byte)
	{
		byte[] array = new byte[buffer.Length];
		for (int i = 0; i < buffer.Length; i++)
		{
			array[i] = (byte)(@byte ^ buffer[i]);
		}
		return array;
	}

	public static byte[] Xor(this byte[] buffer, byte[] bytes)
	{
		byte[] array = new byte[buffer.Length];
		for (int i = 0; i < buffer.Length && i < bytes.Length; i++)
		{
			array[i] = (byte)(bytes[i] ^ buffer[i]);
		}
		return array;
	}

	public static string ToHex(this byte[] bytes, bool toUpper = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in bytes)
		{
			stringBuilder.AppendFormat(toUpper ? "{0:X2}" : "{0:x2}", b);
		}
		return stringBuilder.ToString();
	}

	public static string ToBinary(this byte[] buffer)
	{
		StringBuilder stringBuilder = new StringBuilder(buffer.Length * 8);
		for (int i = 0; i < buffer.Length; i++)
		{
			string text = Convert.ToString(buffer[i], 2);
			for (int j = 0; j < 8 - text.Length; j++)
			{
				stringBuilder.Append('0');
			}
			stringBuilder.Append(text);
		}
		return stringBuilder.ToString();
	}

	public static int bytesToInt(byte[] src, int offset)
	{
		return (src[offset] & 0xFF) | ((src[offset + 1] & 0xFF) << 8) | ((src[offset + 2] & 0xFF) << 16) | ((src[offset + 3] & 0xFF) << 24);
	}

	public static int bytesToInt2(byte[] src, int offset)
	{
		return ((src[offset] & 0xFF) << 24) | ((src[offset + 1] & 0xFF) << 16) | ((src[offset + 2] & 0xFF) << 8) | (src[offset + 3] & 0xFF);
	}

	public static byte[] intToBytes(int value)
	{
		byte[] array = new byte[4];
		array[3] = (byte)((value >> 24) & 0xFF);
		array[2] = (byte)((value >> 16) & 0xFF);
		array[1] = (byte)((value >> 8) & 0xFF);
		array[0] = (byte)(value & 0xFF);
		return array;
	}

	public static byte[] intToBytes2(int value)
	{
		return new byte[4]
		{
			(byte)((value >> 24) & 0xFF),
			(byte)((value >> 16) & 0xFF),
			(byte)((value >> 8) & 0xFF),
			(byte)(value & 0xFF)
		};
	}
}
