using System.Buffers;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Aseprite.CSharp.Readers;

internal static class AsepriteCompressionReader
{
	public static ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> memory)
	{
		// ─── Fast-exit ────────────────────────────────────────────────────────────────
		if (memory.IsEmpty)
			return ReadOnlyMemory<byte>.Empty;

		// ─── Wrap the source in a stream without copying if possible ────────────────
		Stream input;
		if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> seg)  // backed by managed array?
			&& seg.Offset == 0 && seg.Count == memory.Length)
		{
			input = new MemoryStream(seg.Array!, writable: false);        // zero-copy
		}
		else
		{
			// e.g. stackalloc / native buffer – fall back to one defensive copy
			input = new MemoryStream(memory.ToArray(), writable: false);
		}

		// ─── Decompress ──────────────────────────────────────────────────────────────
		using (input)
		{
			using var zlib = new ZLibStream(input, CompressionMode.Decompress);   // .NET 7+
			using var output = new MemoryStream();                                // grows on demand

			const int BufSize = 16 * 1024;    // 16 KiB – good balance for zlib
			byte[] buf = ArrayPool<byte>.Shared.Rent(BufSize);
			try
			{
				int read;
				while ((read = zlib.Read(buf, 0, buf.Length)) > 0)
					output.Write(buf, 0, read);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buf);
			}

			// MemoryStream.ToArray() returns the exact length; caller gets a RO slice.
			return output.ToArray();   // one final, unavoidable copy for the caller’s ReadOnlyMemory
		}
	}

	public static int DecompressInto(ReadOnlyMemory<byte> memory, byte[] buffer, int offset, int length)
	{
		// ─── Fast-exit ────────────────────────────────────────────────────────────────
		if (memory.IsEmpty)
			return 0;

		// ─── Wrap the source in a stream without copying if possible ────────────────
		Stream input;
		if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> seg)  // backed by managed array?
			&& seg.Offset == 0 && seg.Count == memory.Length)
		{
			input = new MemoryStream(seg.Array!, writable: false);        // zero-copy
		}
		else
		{
			// e.g. stackalloc / native buffer – fall back to one defensive copy
			input = new MemoryStream(memory.ToArray(), writable: false);
		}

		// ─── Decompress ──────────────────────────────────────────────────────────────
		using (input)
		{
			using var zlib = new ZLibStream(input, CompressionMode.Decompress);   // .NET 7+

			int readTotal = 0;
			int read;
			while ((read = zlib.Read(buffer, offset + readTotal, length - readTotal)) > 0)
				readTotal += read;

			return readTotal;
		}
	}
}
