using System;
using System.Security.Cryptography;

namespace Login.NetEase.LinkService;

internal class RSAHelper
{
	public static byte[] RSADecrypt(string xmlPrivateKey, byte[] decryptString)
	{
		try
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString(xmlPrivateKey);
			return rSACryptoServiceProvider.Decrypt(decryptString, fOAEP: false);
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	public static byte[] RSAEncrypt(string xmlPublicKey, byte[] EncryptString)
	{
		try
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString(xmlPublicKey);
			return rSACryptoServiceProvider.Encrypt(EncryptString, fOAEP: false);
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}
}
