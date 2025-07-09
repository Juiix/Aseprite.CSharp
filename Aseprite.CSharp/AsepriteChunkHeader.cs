namespace Aseprite.CSharp;

/// <summary>Raw chunk header (size+type).  The size **includes** these 6 bytes.</summary>
internal readonly record struct AsepriteChunkHeader(
	uint SizeInBytes,
	ChunkType Type)
{
	/// <summary>Number of data bytes that follow this header.</summary>
	public int DataLength => checked((int)SizeInBytes) - 6;
}
