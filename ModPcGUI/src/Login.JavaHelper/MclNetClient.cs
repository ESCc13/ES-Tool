using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mcl.Core.Utils;

namespace Login.JavaHelper;

internal class MclNetClient
{
	public static string GetEncryptToken(string Token)
	{
		string randomString = GetRandomString(8);
		string randomString2 = GetRandomString(8);
		string s = randomString + Token + randomString2;
		string s2 = "debbde3548928fab";
		string s3 = "afd4c5c5a7c456a1";
		return AESHelper.BytesToHex(AESHelper.AESEncrypt128Ex(Encoding.ASCII.GetBytes(s), Encoding.ASCII.GetBytes(s2), Encoding.ASCII.GetBytes(s3)));
	}

	public static string GetDecryptToken(string DToken)
	{
		return Encoding.ASCII.GetString(AESHelper.AESDecrypt128Ex(AESHelper.HexToBytes(DToken), Encoding.ASCII.GetBytes("debbde3548928fab"), Encoding.ASCII.GetBytes("afd4c5c5a7c456a1")).Skip(8).Take(16)
			.ToArray());
	}

	public static string GetRandomString(int length, bool useNum = true, bool useLow = true, bool useUpp = true, string custom = "")
	{
		byte[] array = new byte[4];
		new RNGCryptoServiceProvider().GetBytes(array);
		Random random = new Random(BitConverter.ToInt32(array, 0));
		string text = null;
		string text2 = custom;
		if (useNum)
		{
			text2 += "0123456789";
		}
		if (useLow)
		{
			text2 += "abcdefghijklmnopqrstuvwxyz";
		}
		if (useUpp)
		{
			text2 += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		}
		for (int i = 0; i < length; i++)
		{
			text += text2.Substring(random.Next(0, text2.Length - 1), 1);
		}
		return text;
	}
}
