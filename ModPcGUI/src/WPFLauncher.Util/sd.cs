using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WPFLauncher.Util;

public class sd
{
	public unsafe static string a(string fml, string fmm, string fmn)
	{
		string result;
		if (string.IsNullOrEmpty(fml) || string.IsNullOrEmpty(fmn))
		{
			result = fmn;
		}
		else
		{
			byte[] bytes = Encoding.UTF8.GetBytes(fml);
			byte[] array = Encoding.UTF8.GetBytes(fmm);
			byte[] bytes2 = Encoding.UTF8.GetBytes(fmn);
			int num = array.Length;
			if (num == 0)
			{
				array = new byte[1];
			}
			fixed (byte* fkf = &bytes[0])
			{
				fixed (byte* fkg = &array[0])
				{
					fixed (byte* fkh = &bytes2[0])
					{
						IntPtr fki = IntPtr.Zero;
						int num2 = sb.k(fkf, fkg, fkh, out fki, num);
						if (num2 != 0 && fki != IntPtr.Zero)
						{
							byte[] array2 = new byte[num2];
							Marshal.Copy(fki, array2, 0, num2);
							g(fki);
							result = Encoding.UTF8.GetString(array2);
						}
						else
						{
							result = fmn;
						}
					}
				}
			}
		}
		return result;
	}

	public unsafe static byte[] b(string fmo, string fmp, out string fmq)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(fmp);
		byte[] result;
		if (string.IsNullOrEmpty(fmp))
		{
			fmq = string.Empty;
			result = bytes;
		}
		else
		{
			fixed (byte* fkb = &Encoding.UTF8.GetBytes(fmo)[0])
			{
				fixed (byte* fkc = &bytes[0])
				{
					IntPtr fkd = IntPtr.Zero;
					IntPtr fke = IntPtr.Zero;
					int num = sb.j(fkb, fkc, out fkd, out fke);
					byte[] array = new byte[num];
					byte[] array2 = new byte[16];
					if (num != 0 || fkd != IntPtr.Zero)
					{
						Marshal.Copy(fkd, array, 0, num);
						g(fkd);
					}
					if (fke != IntPtr.Zero)
					{
						Marshal.Copy(fke, array2, 0, array2.Length);
						g(fke);
					}
					fmq = Encoding.UTF8.GetString(array2);
					result = array;
				}
			}
		}
		return result;
	}

	public static string c(byte[] fmr, out string fms)
	{
		if (fmr == null || fmr.Length == 0)
		{
			fms = string.Empty;
			return string.Empty;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fmr[0]) * fmr.Length);
		string result = string.Empty;
		fms = string.Empty;
		int fkx = fmr.Length;
		try
		{
			Marshal.Copy(fmr, 0, intPtr, fmr.Length);
			IntPtr fky = IntPtr.Zero;
			IntPtr fkz = IntPtr.Zero;
			int num = sb.p(intPtr, fkx, out fky, out fkz);
			if (num != 0 && fky != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(fky, array, 0, num);
				g(fky);
				result = Encoding.UTF8.GetString(array);
			}
			if (fkz != IntPtr.Zero)
			{
				byte[] array2 = new byte[16];
				Marshal.Copy(fkz, array2, 0, array2.Length);
				g(fkz);
				fms = Encoding.UTF8.GetString(array2);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
		return result;
	}

	public static string d(byte[] fmt, out string fmu)
	{
		if (fmt == null || fmt.Length == 0)
		{
			fmu = string.Empty;
			return string.Empty;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(fmt[0]) * fmt.Length);
		string result = string.Empty;
		fmu = string.Empty;
		int fkl = fmt.Length;
		try
		{
			Marshal.Copy(fmt, 0, intPtr, fmt.Length);
			IntPtr fkm = IntPtr.Zero;
			IntPtr fkn = IntPtr.Zero;
			int num = sb.l(intPtr, fkl, out fkm, out fkn);
			if (num != 0 && fkm != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(fkm, array, 0, num);
				g(fkm);
				result = Encoding.UTF8.GetString(array);
			}
			if (fkn != IntPtr.Zero)
			{
				byte[] array2 = new byte[16];
				Marshal.Copy(fkn, array2, 0, array2.Length);
				g(fkn);
				fmu = Encoding.UTF8.GetString(array2);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
		return result;
	}

	public static string e(string fmv, string fmw)
	{
		byte[] array = null;
		byte[] array2 = null;
		int num = 0;
		int num2 = 0;
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		if (!string.IsNullOrEmpty(fmv))
		{
			if (!fmv.StartsWith("/"))
			{
				fmv = "/" + fmv;
			}
			array = Encoding.UTF8.GetBytes(fmv);
			num2 = Marshal.SizeOf(array[0]) * array.Length;
			intPtr2 = Marshal.AllocHGlobal(num2);
		}
		if (!string.IsNullOrEmpty(fmw))
		{
			array2 = Encoding.UTF8.GetBytes(fmw);
			num = Marshal.SizeOf(array2[0]) * array2.Length;
			intPtr = Marshal.AllocHGlobal(num);
		}
		string result = string.Empty;
		try
		{
			if (array2 != null)
			{
				Marshal.Copy(array2, 0, intPtr, array2.Length);
			}
			if (array != null)
			{
				Marshal.Copy(array, 0, intPtr2, array.Length);
			}
			IntPtr fks = IntPtr.Zero;
			int num3 = sb.m(intPtr2, num2, intPtr, num, out fks);
			if (num3 != 0 && fks != IntPtr.Zero)
			{
				byte[] array3 = new byte[num3];
				Marshal.Copy(fks, array3, 0, num3);
				g(fks);
				result = Encoding.UTF8.GetString(array3);
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr2);
			}
		}
		return result;
	}

	public static string f()
	{
		string result = string.Empty;
		try
		{
			IntPtr fkv = IntPtr.Zero;
			IntPtr fku = IntPtr.Zero;
			int num = sb.o(out fku, out fkv);
			if (num != 0 || fkv != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(fkv, array, 0, num);
				g(fku);
				g(fkv);
				result = Encoding.UTF8.GetString(array);
			}
		}
		catch (Exception)
		{
		}
		return result;
	}

	public static void g(IntPtr fmx)
	{
		if (!(fmx == IntPtr.Zero))
		{
			sb.u(fmx);
		}
	}

	public static int h(string fmy, string fmz, out string fna)
	{
		fna = string.Empty;
		int result = 0;
		try
		{
			result = sb.i(fmy, fmz, out var fjz, out var fka);
			byte[] array = new byte[4];
			if (fka != IntPtr.Zero)
			{
				Marshal.Copy(fka, array, 0, 4);
				g(fka);
			}
			int num = BitConverter.ToInt32(array, 0);
			byte[] array2 = new byte[num];
			if (fjz != IntPtr.Zero)
			{
				Marshal.Copy(fjz, array2, 0, num);
				g(fjz);
			}
			fna = Encoding.UTF8.GetString(array2);
		}
		catch (Exception)
		{
		}
		return result;
	}

	public unsafe static bool i(string fnb, uint fnc)
	{
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(fnb);
			int flo = bytes.Length;
			try
			{
				fixed (byte* fln = &bytes[0])
				{
					int num = sb.v(fln, flo, fnc);
					if (num != 200)
					{
						Term.WriteLine("Error code:" + num);
					}
				}
			}
			finally
			{
				byte* ptr = null;
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public unsafe static void j(string fnd)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(fnd);
		int length = fnd.Length;
		fixed (byte* flq = &bytes[0])
		{
			sb.w(flq, length);
		}
	}

	public unsafe static int k(string fne)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(fne);
		int length = fne.Length;
		fixed (byte* fls = &bytes[0])
		{
			return sb.x(fls, length);
		}
	}
}
