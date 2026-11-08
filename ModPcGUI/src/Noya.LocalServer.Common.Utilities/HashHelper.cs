using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Noya.LocalServer.Common.Extensions;

namespace Noya.LocalServer.Common.Utilities;

public static class HashHelper
{
	private static readonly ThreadLocal<MD5> md5 = new ThreadLocal<MD5>(MD5.Create);

	private static readonly ThreadLocal<SHA256> sha256 = new ThreadLocal<SHA256>(SHA256.Create);

	public static string SafeCompleteMD5Hex(byte[] bytes)
	{
		try
		{
			byte[] array = MD5.Create().ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder();
			byte[] array2 = array;
			byte[] array3 = array2;
			foreach (byte b in array3)
			{
				stringBuilder.Append(b.ToString("x2"));
			}
			return stringBuilder.ToString();
		}
		catch
		{
		}
		return null;
	}

	public static byte[] SafeCompleteSha256(string str, Encoding encoding = null)
	{
		if (encoding == null)
		{
			encoding = Encoding.UTF8;
		}
		try
		{
			return sha256.Value.ComputeHash(encoding.GetBytes(str));
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static byte[] SafeCompleteMD5FromFile(string filePath)
	{
		try
		{
			return md5.Value.CompleteMD5FromFile(filePath);
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static byte[] SafeCompleteMD5(Stream stream)
	{
		try
		{
			return md5.Value.ComputeHash(stream);
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static byte[] SafeCompleteMD5(byte[] bytes)
	{
		try
		{
			return md5.Value.ComputeHash(bytes);
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static byte[] SafeCompleteMD5(string str, Encoding encoding = null)
	{
		if (encoding == null)
		{
			encoding = Encoding.UTF8;
		}
		try
		{
			return md5.Value.ComputeHash(encoding.GetBytes(str));
		}
		catch (Exception)
		{
		}
		return null;
	}

	public static string GetMD5WithString(string sDataIn)
	{
		string text = "";
		byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(text);
		byte[] array = new MD5CryptoServiceProvider().ComputeHash(bytes);
		for (int i = 0; i < array.Length; i++)
		{
			text += array[i].ToString("x2");
		}
		return text;
	}
}
