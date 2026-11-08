using System.Runtime.InteropServices;

namespace Login.Utils;

public class EncryptionHelper
{
	private byte[] key = new byte[10];

	public EncryptionHelper(byte[] key)
	{
		key.CopyTo(this.key, 0);
	}

	public unsafe uint Encrypt(uint w)
	{
		fixed (byte* ptr = &key[0])
		{
			return Skip32(en: true, ptr, w);
		}
	}

	public unsafe uint Decrypt(uint w)
	{
		fixed (byte* ptr = &key[0])
		{
			return Skip32(en: false, ptr, w);
		}
	}

	public unsafe static bool CheckToken(byte[] token, byte[] key)
	{
		fixed (byte* data = &token[0])
		{
			fixed (byte* ptr = &key[0])
			{
				return CheckToken(data, 32u, ptr, 16u);
			}
		}
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern uint _ff487957b05f3b54712db300a8687189(bool en, byte* key, uint word);

	public unsafe static uint Skip32(bool en, byte* key, uint word)
	{
		return _ff487957b05f3b54712db300a8687189(en, key, word);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern bool _1b559cbb2d1a2c82336de16a49adc867(byte* data, uint len, byte* key, uint keylen);

	public unsafe static bool CheckToken(byte* data, uint len, byte* key, uint keylen)
	{
		return _1b559cbb2d1a2c82336de16a49adc867(data, len, key, keylen);
	}
}
