using System;
using System.Collections.Generic;
using System.Text;

namespace WPFLauncher.Util;

public static class Encrypt
{
	private const long s = 2654435769L;

	private const int i = 32;

	private const char j = '\0';

	public static string a(this string gig, string gih)
	{
		return c(Encoding.UTF8.GetBytes(gig.PadRight(32, '\0')).e(), Encoding.UTF8.GetBytes(gih.PadRight(32, '\0')).e()).g();
	}

	public static string b(this string gii, string gij)
	{
		if (string.IsNullOrWhiteSpace(gii))
		{
			return gii;
		}
		byte[] array = d(gii.h(), Encoding.UTF8.GetBytes(gij.PadRight(32, '\0')).e()).f();
		return Encoding.UTF8.GetString(array, 0, array.Length);
	}

	private static long[] c(long[] gik, long[] gil)
	{
		int num = gik.Length;
		if (num < 1)
		{
			return gik;
		}
		long num2 = gik[gik.Length - 1];
		long num3 = gik[0];
		long num4 = 0L;
		long num5 = 6 + 52 / num;
		while (num5-- > 0)
		{
			num4 += 2654435769u;
			long num6 = (num4 >> 2) & 3;
			long num7;
			for (num7 = 0L; num7 < num - 1; num7++)
			{
				num3 = gik[(int)(IntPtr)(num7 + 1)];
				num2 = (gik[(int)(IntPtr)num7] += (((num2 >> 5) ^ (num3 << 2)) + ((num3 >> 3) ^ (num2 << 4))) ^ ((num4 ^ num3) + (gil[(int)(IntPtr)((num7 & 3) ^ num6)] ^ num2)));
			}
			num3 = gik[0];
			num2 = (gik[num - 1] += (((num2 >> 5) ^ (num3 << 2)) + ((num3 >> 3) ^ (num2 << 4))) ^ ((num4 ^ num3) + (gil[(int)(IntPtr)((num7 & 3) ^ num6)] ^ num2)));
		}
		return gik;
	}

	private static long[] d(long[] gim, long[] gin)
	{
		int num = gim.Length;
		if (num < 1)
		{
			return gim;
		}
		long num2 = gim[gim.Length - 1];
		long num3 = gim[0];
		for (long num4 = (6 + 52 / num) * 2654435769u; num4 != 0; num4 -= 2654435769u)
		{
			long num5 = (num4 >> 2) & 3;
			long num6;
			for (num6 = num - 1; num6 > 0; num6--)
			{
				num2 = gim[(int)(IntPtr)(num6 - 1)];
				num3 = (gim[(int)(IntPtr)num6] -= (((num2 >> 5) ^ (num3 << 2)) + ((num3 >> 3) ^ (num2 << 4))) ^ ((num4 ^ num3) + (gin[(int)(IntPtr)((num6 & 3) ^ num5)] ^ num2)));
			}
			num2 = gim[num - 1];
			num3 = (gim[0] -= (((num2 >> 5) ^ (num3 << 2)) + ((num3 >> 3) ^ (num2 << 4))) ^ ((num4 ^ num3) + (gin[(int)(IntPtr)((num6 & 3) ^ num5)] ^ num2)));
		}
		return gim;
	}

	private static long[] e(this byte[] gio)
	{
		int num = ((gio.Length % 8 != 0) ? 1 : 0) + gio.Length / 8;
		long[] array = new long[num];
		for (int i = 0; i < num - 1; i++)
		{
			array[i] = BitConverter.ToInt64(gio, i * 8);
		}
		byte[] array2 = new byte[8];
		Array.Copy(gio, (num - 1) * 8, array2, 0, gio.Length - (num - 1) * 8);
		array[num - 1] = BitConverter.ToInt64(array2, 0);
		return array;
	}

	private static byte[] f(this long[] gip)
	{
		List<byte> list = new List<byte>(gip.Length * 8);
		for (int i = 0; i < gip.Length; i++)
		{
			list.AddRange(BitConverter.GetBytes(gip[i]));
		}
		while (list[list.Count - 1] == 0)
		{
			list.RemoveAt(list.Count - 1);
		}
		return list.ToArray();
	}

	private static string g(this long[] giq)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < giq.Length; i++)
		{
			stringBuilder.Append(giq[i].ToString("x2").PadLeft(16, '0'));
		}
		return stringBuilder.ToString();
	}

	private static long[] h(this string gir)
	{
		int num = gir.Length / 16;
		long[] array = new long[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = Convert.ToInt64(gir.Substring(i * 16, 16), 16);
		}
		return array;
	}
}
