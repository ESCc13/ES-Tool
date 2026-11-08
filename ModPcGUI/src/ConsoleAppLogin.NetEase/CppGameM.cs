using System;
using System.Linq;
using MCStudio.Network.Http;

namespace ConsoleAppLogin.NetEase;

internal class CppGameM
{
	public static string GetLoginToken()
	{
		return Convert.ToBase64String(HexStringToByteArray(CoreNative.GetH5Token()));
	}

	public static byte[] GetLoginTokenBytes()
	{
		return HexStringToByteArray(CoreNative.GetH5Token());
	}

	public static byte[] HexStringToByteArray(string hex)
	{
		return (from x in Enumerable.Range(0, hex.Length)
			where x % 2 == 0
			select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
	}
}
