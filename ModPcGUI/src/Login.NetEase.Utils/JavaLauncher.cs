using System;
using System.Security.Cryptography;
using System.Text;
using ConsoleAppLogin.NetEase;
using Login.Utils;
using Mcl.Core.Utils;

namespace Login.NetEase.Utils;

internal class JavaLauncher
{
	private static byte[] Skip32Key = Encoding.UTF8.GetBytes("SaintSteve");

	private static EncryptionHelper skip32 = new EncryptionHelper(Skip32Key);

	public static string GenerateUUID(string roleName)
	{
		byte[] array = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(roleName));
		byte[] bytes = BitConverter.GetBytes(skip32.Encrypt(NumberHelper.StringToUInt(Http.UID)));
		Buffer.BlockCopy(bytes, 0, array, 12, bytes.Length);
		int num = 6;
		array[num] &= 15;
		int num2 = 6;
		array[num2] |= 64;
		int num3 = 8;
		array[num3] &= 63;
		int num4 = 8;
		array[num4] |= 128;
		return AESHelper.BytesToHex(array);
	}
}
