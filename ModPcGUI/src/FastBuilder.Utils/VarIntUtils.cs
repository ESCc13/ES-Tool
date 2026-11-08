using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace FastBuilder.Utils;

public static class VarIntUtils
{
	public static byte[] GetVarInt(uint value)
	{
		byte[] array = new byte[5];
		Stream stream = new MemoryStream(array);
		while ((value & -128) != 0)
		{
			stream.WriteByte((byte)((value & 0x7F) | 0x80));
			value >>= 7;
		}
		stream.WriteByte((byte)value);
		byte[] array2 = new byte[stream.Position];
		Array.Copy(array, array2, stream.Position);
		return array2;
	}

	public static uint ReadVarUInt32(BinaryReader buf)
	{
		uint num = 0u;
		byte b = buf.ReadByte();
		int num2 = 0;
		do
		{
			num |= (uint)((b & 0x7F) << num2++ * 7);
		}
		while ((b & 0x80) != 0);
		return num;
	}

	public static byte[] Counter(byte[] value)
	{
		byte[] array = new byte[value.Length];
		for (int i = 0; value.Length > i; i++)
		{
			array[i] = value[value.Length - i - 1];
		}
		return array;
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

	public static int bytesToInt(byte[] src, int offset)
	{
		return (src[offset] & 0xFF) | ((src[offset + 1] & 0xFF) << 8) | ((src[offset + 2] & 0xFF) << 16) | ((src[offset + 3] & 0xFF) << 24);
	}

	public static uint bytesToInt24Counter(byte[] src, int offset)
	{
		return BitConverter.ToUInt32(new byte[4]
		{
			0,
			src[offset + 2],
			src[offset + 1],
			src[offset]
		}, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint EncodeZigZag32(int n)
	{
		return (uint)((n << 1) ^ (n >> 31));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int DecodeZigZag32(uint n)
	{
		return (int)((n >> 1) ^ (0 - (n & 1)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong EncodeZigZag64(long n)
	{
		return (ulong)((n << 1) ^ (n >> 63));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long DecodeZigZag64(ulong n)
	{
		return (long)((n >> 1) ^ (0 - (n & 1)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ReadRawVarInt32(Stream buf, int maxSize)
	{
		uint num = 0u;
		int num2 = 0;
		int num3;
		do
		{
			num3 = buf.ReadByte();
			if (num3 < 0)
			{
				throw new EndOfStreamException("Not enough bytes for VarInt");
			}
			num |= (uint)((num3 & 0x7F) << num2++ * 7);
			if (num2 > maxSize)
			{
				throw new OverflowException("VarInt too big");
			}
		}
		while ((num3 & 0x80) == 128);
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong ReadRawVarInt64(Stream buf, int maxSize, bool printBytes = false)
	{
		List<byte> list = new List<byte>();
		ulong num = 0uL;
		int num2 = 0;
		int num3;
		do
		{
			num3 = buf.ReadByte();
			list.Add((byte)num3);
			if (num3 < 0)
			{
				throw new EndOfStreamException("Not enough bytes for VarInt");
			}
			num |= (ulong)((long)(num3 & 0x7F) << num2++ * 7);
			if (num2 > maxSize)
			{
				throw new OverflowException("VarInt too big");
			}
		}
		while ((num3 & 0x80) == 128);
		list.ToArray();
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteRawVarInt32(Stream buf, uint value)
	{
		while ((value & -128) != 0)
		{
			buf.WriteByte((byte)((value & 0x7F) | 0x80));
			value >>= 7;
		}
		buf.WriteByte((byte)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteRawVarInt64(Stream buf, ulong value)
	{
		while ((value & 0xFFFFFFFFFFFFFF80uL) != 0)
		{
			buf.WriteByte((byte)((value & 0x7F) | 0x80));
			value >>= 7;
		}
		buf.WriteByte((byte)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt32(Stream stream, int value)
	{
		WriteRawVarInt32(stream, (uint)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadInt32(Stream stream)
	{
		return (int)ReadRawVarInt32(stream, 5);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSInt32(Stream stream, int value)
	{
		WriteRawVarInt32(stream, EncodeZigZag32(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadSInt32(Stream stream)
	{
		return DecodeZigZag32(ReadRawVarInt32(stream, 5));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteUInt32(Stream stream, uint value)
	{
		WriteRawVarInt32(stream, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ReadUInt32(Stream stream)
	{
		return ReadRawVarInt32(stream, 5);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt64(Stream stream, long value)
	{
		WriteRawVarInt64(stream, (ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadInt64(Stream stream, bool printBytes = false)
	{
		return (long)ReadRawVarInt64(stream, 10, printBytes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSInt64(Stream stream, long value)
	{
		WriteRawVarInt64(stream, EncodeZigZag64(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadSInt64(Stream stream)
	{
		return DecodeZigZag64(ReadRawVarInt64(stream, 10));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteUInt64(Stream stream, ulong value)
	{
		WriteRawVarInt64(stream, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ReadUInt64(Stream stream)
	{
		return ReadRawVarInt64(stream, 10);
	}
}
