using System.IO;
using System.IO.Compression;

namespace FastBuilder.Utils;

internal class ZlibUtils
{
	public static byte[] MicrosoftCompress(byte[] data)
	{
		MemoryStream memoryStream = new MemoryStream(data);
		MemoryStream memoryStream2 = new MemoryStream();
		DeflateStream deflateStream = new DeflateStream(memoryStream2, CompressionLevel.Fastest);
		memoryStream.CopyTo(deflateStream);
		deflateStream.Close();
		return memoryStream2.ToArray();
	}

	public static byte[] MicrosoftDecompress(byte[] data)
	{
		MemoryStream stream = new MemoryStream(data);
		MemoryStream memoryStream = new MemoryStream();
		new DeflateStream(stream, CompressionMode.Decompress).CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public static byte[] MicrosoftZlibCompress(byte[] data)
	{
		MemoryStream memoryStream = new MemoryStream(data);
		MemoryStream memoryStream2 = new MemoryStream();
		DeflateStream deflateStream = new DeflateStream(memoryStream2, CompressionMode.Compress);
		memoryStream.CopyTo(deflateStream);
		deflateStream.Close();
		return memoryStream2.ToArray();
	}

	public static byte[] MicrosoftZlibDecompress(byte[] data)
	{
		MemoryStream stream = new MemoryStream(data);
		MemoryStream memoryStream = new MemoryStream();
		new DeflateStream(stream, CompressionMode.Decompress).CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
