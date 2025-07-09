namespace Aseprite.CSharp.Readers;

internal static class AsepriteChunkHeaderReader
{
	/// <summary>
	/// Reads the 6-byte chunk header.
	/// Throws if <c>SizeInBytes &lt; 6</c> (an invalid chunk).
	/// </summary>
	public static AsepriteChunkHeader Read(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);
		uint size = br.ReadDWord();             // total chunk size (>= 6)
		if (size < 6)
			throw new InvalidDataException($"Invalid chunk size: {size} (must be >= 6 bytes).");

		ChunkType type = (ChunkType)br.ReadWord();              // chunk type id

		// reader now points at the chunk payload
		return new AsepriteChunkHeader(size, type);
	}
}