using System;
using System.Runtime.InteropServices;

namespace MCStudio.Utils;

public class ChaChaX
{
	private IntPtr ctx;

	public const string CORE_DLL_NAME = "mcl.common.dll";

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern IntPtr _51258412ae7f26a1cbfcfc4c52b215cb(uint lv, byte* key);

	public unsafe static IntPtr NewChaCha(uint lv, byte* key)
	{
		return _51258412ae7f26a1cbfcfc4c52b215cb(lv, key);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern void _b71fa6b924744e4fdf5091006d3ac0c8(IntPtr ctx);

	public static void DeleteChaCha(IntPtr ctx)
	{
		_b71fa6b924744e4fdf5091006d3ac0c8(ctx);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern void _b79c5024733866f2a0d68ae29f94b595(IntPtr ctx, byte* data, uint len);

	public unsafe static void ChaChaProcess(IntPtr ctx, byte* data, uint len)
	{
		_b79c5024733866f2a0d68ae29f94b595(ctx, data, len);
	}

	protected unsafe ChaChaX(uint lv, byte[] key)
	{
		fixed (byte* key2 = &key[0])
		{
			ctx = NewChaCha(lv, key2);
		}
	}

	~ChaChaX()
	{
		DeleteChaCha(ctx);
	}

	public void Delete()
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
