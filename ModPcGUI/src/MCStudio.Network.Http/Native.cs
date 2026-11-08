using System;
using System.Runtime.InteropServices;

namespace MCStudio.Network.Http;

internal class Native
{
	public const string CORE_DLL_NAME = "mcl.common.dll";

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public static extern int CanLaunch(string flz, string fma, out IntPtr fmb, out IntPtr fmc);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
	public unsafe static extern int HttpEncrypt(byte* url, byte* body, out IntPtr buff, out IntPtr key);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public unsafe static extern int ComputeSilence(byte* url, byte* body, byte* data, out IntPtr buff, int bodyLen);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public static extern int ParseLoginResponse(IntPtr pArray, int nSize, out IntPtr buff, out IntPtr key);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public static extern int ComputeDynamicToken(IntPtr urlPtr, int urlSz, IntPtr bodyPtr, int bodySz, out IntPtr buff);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public static extern int GetH5Token(out IntPtr valPtr, out IntPtr buff);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public static extern int HttpDecrypt(IntPtr pArray, int nSize, out IntPtr buff, out IntPtr key);

	[DllImport("mcl.common.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public static extern void FreeMemory(IntPtr ptr);
}
