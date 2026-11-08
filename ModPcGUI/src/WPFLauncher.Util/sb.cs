using System;
using System.Runtime.InteropServices;

namespace WPFLauncher.Util;

public class sb
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public unsafe delegate int a(byte* flw, byte* flx, out IntPtr fly, out IntPtr flz);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int b(IntPtr fmc, int fmd, out IntPtr fme, out IntPtr fmf);

	public const string a_ = "mcl.common.dll";

	public const string b_ = "mcl.common.dll";

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern IntPtr _51258412ae7f26a1cbfcfc4c52b215cb(uint fjf, byte* fjg);

	public unsafe static IntPtr B(uint fjh, byte* fji)
	{
		return _51258412ae7f26a1cbfcfc4c52b215cb(fjh, fji);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_b71fa6b924744e4fdf5091006d3ac0c8")]
	private static extern void c(IntPtr fjj);

	public static void d(IntPtr fjk)
	{
		c(fjk);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_b79c5024733866f2a0d68ae29f94b595")]
	private unsafe static extern void e(IntPtr fjl, byte* fjm, uint fjn);

	public unsafe static void f(IntPtr fjo, byte* fjp, uint fjq)
	{
		e(fjo, fjp, fjq);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_ff487957b05f3b54712db300a8687189")]
	private unsafe static extern uint g(bool fjr, byte* fjs, uint fjt);

	public unsafe static uint h(bool fju, byte* fjv, uint fjw)
	{
		return g(fju, fjv, fjw);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CanLaunch")]
	public static extern int i(string fjx, string fjy, out IntPtr fjz, out IntPtr fka);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "HttpEncrypt")]
	public unsafe static extern int j(byte* fkb, byte* fkc, out IntPtr fkd, out IntPtr fke);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "ComputeSilence")]
	public unsafe static extern int k(byte* fkf, byte* fkg, byte* fkh, out IntPtr fki, int fkj);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "ParseLoginResponse")]
	public static extern int l(IntPtr fkk, int fkl, out IntPtr fkm, out IntPtr fkn);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "ComputeDynamicToken")]
	public static extern int m(IntPtr fko, int fkp, IntPtr fkq, int fkr, out IntPtr fks);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "GetToken")]
	public unsafe static extern int n(byte* fkt);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "GetH5Token")]
	public static extern int o(out IntPtr fku, out IntPtr fkv);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "HttpDecrypt")]
	public static extern int p(IntPtr fkw, int fkx, out IntPtr fky, out IntPtr fkz);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_1b559cbb2d1a2c82336de16a49adc867")]
	private unsafe static extern bool q(byte* fla, uint flb, byte* flc, uint fld);

	public unsafe static bool r(byte* fle, uint flf, byte* flg, uint flh)
	{
		return q(fle, flf, flg, flh);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "_aee7739eac9adbadc28f90f6d9ad984c")]
	private unsafe static extern void s(byte* fli, uint flj);

	public unsafe static void t(byte* flk, uint fll)
	{
		s(flk, fll);
	}

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "FreeMemory")]
	public static extern void u(IntPtr flm);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "SetUid")]
	public unsafe static extern int v(byte* fln, int flo, uint flp);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "GetItemInfo")]
	public unsafe static extern int w(byte* flq, int flr);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "UnzipModJson")]
	public unsafe static extern int x(byte* fls, int flt);
}
