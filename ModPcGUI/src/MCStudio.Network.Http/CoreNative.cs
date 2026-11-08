using System;
using System.Runtime.InteropServices;
using System.Text;
using Login.NetEase;
using Mcl.Core.Utils;

namespace MCStudio.Network.Http;

internal class CoreNative
{
	public unsafe static string ComputeSilence(string url, string body, string data)
	{
		if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(data))
		{
			return data;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(url);
		byte[] array = Encoding.UTF8.GetBytes(body);
		byte[] bytes2 = Encoding.UTF8.GetBytes(data);
		int num = array.Length;
		if (num == 0)
		{
			array = new byte[1];
		}
		fixed (byte* url2 = &bytes[0])
		{
			fixed (byte* body2 = &array[0])
			{
				fixed (byte* data2 = &bytes2[0])
				{
					IntPtr buff = IntPtr.Zero;
					int num2 = Native.ComputeSilence(url2, body2, data2, out buff, num);
					if (num2 != 0 && buff != IntPtr.Zero)
					{
						byte[] array2 = new byte[num2];
						Marshal.Copy(buff, array2, 0, num2);
						FreeMemory(buff);
						return Encoding.UTF8.GetString(array2);
					}
				}
			}
		}
		return data;
	}

	public unsafe static byte[] HttpEncrypt(string url, string body, out string keyStr)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(body);
		if (string.IsNullOrEmpty(body))
		{
			keyStr = string.Empty;
			return bytes;
		}
		fixed (byte* url2 = &Encoding.UTF8.GetBytes(url)[0])
		{
			fixed (byte* body2 = &bytes[0])
			{
				IntPtr buff = IntPtr.Zero;
				IntPtr key = IntPtr.Zero;
				int num = Native.HttpEncrypt(url2, body2, out buff, out key);
				byte[] array = new byte[num];
				byte[] array2 = new byte[16];
				if (num != 0 || buff != IntPtr.Zero)
				{
					Marshal.Copy(buff, array, 0, num);
					FreeMemory(buff);
				}
				if (key != IntPtr.Zero)
				{
					Marshal.Copy(key, array2, 0, array2.Length);
					FreeMemory(key);
				}
				keyStr = Encoding.UTF8.GetString(array2);
				return array;
			}
		}
	}

	public static string HttpDecrypt(byte[] body, out string keyStr)
	{
		if (body == null || body.Length == 0)
		{
			keyStr = string.Empty;
			return string.Empty;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(body[0]) * body.Length);
		string result = string.Empty;
		keyStr = string.Empty;
		int nSize = body.Length;
		try
		{
			Marshal.Copy(body, 0, intPtr, body.Length);
			IntPtr buff = IntPtr.Zero;
			IntPtr key = IntPtr.Zero;
			int num = Native.HttpDecrypt(intPtr, nSize, out buff, out key);
			if (num != 0 && buff != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(buff, array, 0, num);
				FreeMemory(buff);
				result = Encoding.UTF8.GetString(array);
			}
			if (key != IntPtr.Zero)
			{
				byte[] array2 = new byte[16];
				Marshal.Copy(key, array2, 0, array2.Length);
				FreeMemory(key);
				keyStr = Encoding.UTF8.GetString(array2);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
		return result;
	}

	public static string ParseLoginResponse(byte[] body, out string keyStr)
	{
		if (body == null || body.Length == 0)
		{
			keyStr = string.Empty;
			return string.Empty;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(body[0]) * body.Length);
		string result = string.Empty;
		keyStr = string.Empty;
		int nSize = body.Length;
		try
		{
			Marshal.Copy(body, 0, intPtr, body.Length);
			IntPtr buff = IntPtr.Zero;
			IntPtr key = IntPtr.Zero;
			int num = Native.ParseLoginResponse(intPtr, nSize, out buff, out key);
			if (num != 0 && buff != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(buff, array, 0, num);
				FreeMemory(buff);
				result = Encoding.UTF8.GetString(array);
			}
			if (key != IntPtr.Zero)
			{
				byte[] array2 = new byte[16];
				Marshal.Copy(key, array2, 0, array2.Length);
				FreeMemory(key);
				keyStr = Encoding.UTF8.GetString(array2);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
		return result;
	}

	public static string ComputeDynamicToken(string urlStr, string bodyStr)
	{
		byte[] array = null;
		byte[] array2 = null;
		int num = 0;
		int num2 = 0;
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		if (!string.IsNullOrEmpty(urlStr))
		{
			array = Encoding.UTF8.GetBytes(urlStr);
			num2 = Marshal.SizeOf(array[0]) * array.Length;
			intPtr2 = Marshal.AllocHGlobal(num2);
		}
		if (!string.IsNullOrEmpty(bodyStr))
		{
			array2 = Encoding.UTF8.GetBytes(bodyStr);
			num = Marshal.SizeOf(array2[0]) * array2.Length;
			intPtr = Marshal.AllocHGlobal(num);
		}
		string result = string.Empty;
		try
		{
			if (array2 != null)
			{
				Marshal.Copy(array2, 0, intPtr, array2.Length);
			}
			if (array != null)
			{
				Marshal.Copy(array, 0, intPtr2, array.Length);
			}
			IntPtr buff = IntPtr.Zero;
			int num3 = Native.ComputeDynamicToken(intPtr2, num2, intPtr, num, out buff);
			if (num3 != 0 && buff != IntPtr.Zero)
			{
				byte[] array3 = new byte[num3];
				Marshal.Copy(buff, array3, 0, num3);
				FreeMemory(buff);
				result = Encoding.UTF8.GetString(array3);
			}
		}
		catch (Exception err)
		{
			Logger.Default.Error(err, "c++ dynamic token");
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr2);
			}
		}
		return result;
	}

	public static string GetH5Token()
	{
		string result = string.Empty;
		try
		{
			IntPtr buff = IntPtr.Zero;
			IntPtr valPtr = IntPtr.Zero;
			int h5Token = Native.GetH5Token(out valPtr, out buff);
			if (h5Token != 0 || buff != IntPtr.Zero)
			{
				byte[] array = new byte[h5Token];
				Marshal.Copy(buff, array, 0, h5Token);
				FreeMemory(valPtr);
				FreeMemory(buff);
				result = Encoding.UTF8.GetString(array);
			}
		}
		catch
		{
			Term.Error.WriteLine("Error:");
		}
		return result;
	}

	public static int CanLaunch(string fpb, string fpc, out string fpd)
	{
		fpd = string.Empty;
		int result = 0;
		try
		{
			result = Native.CanLaunch(fpb, fpc, out var fmb, out var fmc);
			byte[] array = new byte[4];
			if (fmc != IntPtr.Zero)
			{
				Marshal.Copy(fmc, array, 0, 4);
				FreeMemory(fmc);
			}
			int num = BitConverter.ToInt32(array, 0);
			byte[] array2 = new byte[num];
			if (fmb != IntPtr.Zero)
			{
				Marshal.Copy(fmb, array2, 0, num);
				FreeMemory(fmb);
			}
			fpd = Encoding.UTF8.GetString(array2);
		}
		catch (Exception ex)
		{
			Function.ClientLog("[Libary] " + ex.ToString());
		}
		return result;
	}

	public static void FreeMemory(IntPtr ptr)
	{
		if (!(ptr == IntPtr.Zero))
		{
			Native.FreeMemory(ptr);
		}
	}
}
