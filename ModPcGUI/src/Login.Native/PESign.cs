using System;
using System.Runtime.InteropServices;

namespace Login.Native;

internal class PESign
{
	[DllImport("Auth.Sign.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public unsafe static extern IntPtr CountSign(byte* ptr, int size, int offset, int vector);

	[DllImport("Auth.Sign.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	public static extern void FreeMemory(IntPtr ptr);
}
