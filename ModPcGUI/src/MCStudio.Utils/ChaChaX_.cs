using System;
using System.Runtime.InteropServices;

namespace MCStudio.Utils;

public class ChaChaX_
{
	private IntPtr ctx;

	public const string CORE_DLL_NAME = "api-ms-win-crt-utility-l1-1-1.dll";

	[DllImport("api-ms-win-crt-utility-l1-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern IntPtr _51258412ae7f26a1cbfcfc4c52b215cb(uint lv, byte* key);

	public unsafe static IntPtr NewChaCha(uint lv, byte* key)
	{
		return _51258412ae7f26a1cbfcfc4c52b215cb(lv, key);
	}

	[DllImport("api-ms-win-crt-utility-l1-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void _b71fa6b924744e4fdf5091006d3ac0c8(IntPtr ctx);

	public static void DeleteChaCha(IntPtr ctx)
	{
		_b71fa6b924744e4fdf5091006d3ac0c8(ctx);
	}

	[DllImport("api-ms-win-crt-utility-l1-1-1.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern void _b79c5024733866f2a0d68ae29f94b595(IntPtr ctx, byte* data, uint len);

	public unsafe static void ChaChaProcess(IntPtr ctx, byte* data, uint len)
	{
		_b79c5024733866f2a0d68ae29f94b595(ctx, data, len);
	}

	protected unsafe ChaChaX_(uint lv, byte[] key)
	{
		fixed (byte* key2 = &key[0])
		{
			ctx = NewChaCha(lv, key2);
		}
	}

	~ChaChaX_()
	{
		DeleteChaCha(ctx);
	}

	public unsafe void Process(byte[] data)
	{
		if (data.Length != 0)
		{
			fixed (byte* data2 = &data[0])
			{
				ChaChaProcess(ctx, data2, (uint)data.Length);
			}
		}
	}
}
