namespace Aseprite.CSharp;

public sealed record AsepriteFrame
(
	uint BytesInFrame,   // total size of this frame, header included
	ushort Magic,          // must be 0xF1FA
	ushort OldChunkCount,  // 0xFFFF -> "look at NewChunkCount"
	ushort DurationMs,     // frame duration
	uint NewChunkCount   // 0 -> "use OldChunkCount"
) : AsepriteObject
{
	/// <summary>Total number of chunks that follow this header.</summary>
	public int ChunkCount =>
		NewChunkCount != 0 ? (int)NewChunkCount
		: OldChunkCount == 0xFFFF ? throw new InvalidDataException(
			"Chunk count overflow: old count is 0xFFFF and new count is 0.")
		: OldChunkCount;
}

public static class AsepriteFrameReader
{
	/// <summary>
	/// Reads the 16-byte frame header and returns it.
	/// Leaves the reader positioned at the first chunk.
	/// </summary>
	public static AsepriteFrame Read(ReadOnlySpan<byte> span)
	{
		var br = new BinaryReader(span);
		uint sizeInBytes = br.ReadDWord();
		ushort magic = br.ReadWord();           // 0xF1FA
		ushort oldChunkCount = br.ReadWord();
		ushort duration = br.ReadWord();
		br.ReadBytes(2);                                // skip future-use bytes
		uint newChunkCount = br.ReadDWord();

		return new AsepriteFrame(sizeInBytes, magic, oldChunkCount,
							   duration, newChunkCount);
	}
}