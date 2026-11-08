using System;
using System.IO;
using System.Security.Cryptography;

namespace Noya.LocalServer.Common.Cryptography;

public class AESHelper
{
	public static byte[] AES_CBC_Decrypt(byte[] key, byte[] data, byte[] iv)
	{
		using Aes aes = Aes.Create();
		aes.KeySize = key.Length * 8;
		aes.BlockSize = 128;
		aes.Key = key;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.None;
		using ICryptoTransform cryptoTransform = aes.CreateDecryptor();
		byte[] array = new byte[data.Length];
		cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
		return array;
	}

	public static byte[] AES_CBC_Encrypt(byte[] key, byte[] data, byte[] iv)
	{
		using Aes aes = Aes.Create();
		aes.KeySize = key.Length * 8;
		aes.BlockSize = 128;
		aes.Key = key;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.None;
		using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
		byte[] array = new byte[data.Length];
		cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
		return array;
	}

	public static ICryptoTransform getCipherInstance(byte[] Key, bool encrypt = true)
	{
		if (Key.Length < 16)
		{
			Array.Resize(ref Key, 16);
		}
		else if (Key.Length < 24)
		{
			Array.Resize(ref Key, 24);
		}
		else if (Key.Length < 32)
		{
			Array.Resize(ref Key, 32);
		}
		else
		{
			Array.Resize(ref Key, 32);
		}
		RijndaelManaged rijndaelManaged = new RijndaelManaged();
		rijndaelManaged.Mode = CipherMode.ECB;
		rijndaelManaged.KeySize = 128;
		rijndaelManaged.Key = Key;
		rijndaelManaged.Padding = PaddingMode.PKCS7;
		rijndaelManaged.BlockSize = 128;
		if (!encrypt)
		{
			return rijndaelManaged.CreateDecryptor();
		}
		return rijndaelManaged.CreateEncryptor();
	}

	public static byte[] encrypt(byte[] key, byte[] source)
	{
		return getCipherInstance(key).TransformFinalBlock(source, 0, source.Length);
	}

	public static byte[] decrypt(byte[] key, byte[] source)
	{
		return getCipherInstance(key, encrypt: false).TransformFinalBlock(source, 0, source.Length);
	}

	public static byte[] AES_CFB_Decrypt(byte[] key, byte[] data, byte[] iv)
	{
		try
		{
			MemoryStream memoryStream = new MemoryStream(data);
			using Aes aes = Aes.Create();
			aes.Key = key;
			aes.IV = iv;
			aes.Mode = CipherMode.CFB;
			aes.Padding = PaddingMode.Zeros;
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
			try
			{
				byte[] array = new byte[data.Length + 32];
				int num = cryptoStream.Read(array, 0, data.Length + 32);
				byte[] array2 = new byte[num];
				Array.Copy(array, 0, array2, 0, num);
				return array2;
			}
			finally
			{
				cryptoStream.Close();
				memoryStream.Close();
				aes.Clear();
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.Message);
			return null;
		}
	}

	public static byte[] AES_ECB_Encrypt(byte[] key, byte[] data)
	{
		using Aes aes = Aes.Create();
		aes.KeySize = key.Length * 8;
		aes.BlockSize = 128;
		aes.Key = key;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.None;
		using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
		byte[] array = new byte[data.Length];
		cryptoTransform.TransformBlock(data, 0, data.Length, array, 0);
		return array;
	}
}
