using System;
using System.Runtime.InteropServices;
using System.Text;
using text;

namespace Login.Native;

internal class CoreSign
{
	public unsafe static byte[] PESignCount(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return null;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(message);
		int size = bytes.Length;
		int offset = app.Temp.offset;
		int rounds = app.Temp.rounds;
		fixed (byte* ptr = &bytes[0])
		{
			IntPtr zero = IntPtr.Zero;
			zero = PESign.CountSign(ptr, size, offset, rounds);
			if (zero != IntPtr.Zero)
			{
				byte[] array = new byte[16];
				Marshal.Copy(zero, array, 0, 16);
				FreeMemory(zero);
				return array;
			}
			return null;
		}
	}

	public unsafe static byte[] PESignCount(string message, int offset, int rounds)
	{
		if (string.IsNullOrEmpty(message))
		{
			return null;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(message);
		int size = bytes.Length;
		fixed (byte* ptr = &bytes[0])
		{
			IntPtr zero = IntPtr.Zero;
			zero = PESign.CountSign(ptr, size, offset, rounds);
			if (zero != IntPtr.Zero)
			{
				byte[] array = new byte[16];
				Marshal.Copy(zero, array, 0, 16);
				FreeMemory(zero);
				return array;
			}
			return null;
		}
	}

	public static void FreeMemory(IntPtr ptr)
	{
		if (!(ptr == IntPtr.Zero))
		{
			PESign.FreeMemory(ptr);
		}
	}
}
