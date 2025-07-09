using Aseprite.CSharp.Readers;

namespace Aseprite.CSharp;

public sealed record AsepriteFile(
	AsepriteHeader Header,
	ReadOnlyMemory<AsepriteFrame> Frames,
	ReadOnlyMemory<AsepriteLayer> Layers,
	ReadOnlyMemory<ReadOnlyMemory<AsepriteCel?>> Cels,
	AsepriteColorProfile? ColorProfile,
	AsepriteExternalFiles? ExternalFiles,
	ReadOnlyMemory<AsepriteTag> Tags,
	ReadOnlyMemory<PaletteEntry> Palette,
	Dictionary<AsepriteObject, AsepriteUserData> UserDatas,
	ReadOnlyMemory<AsepriteSlice> Slices)
{
	public static Task<AsepriteFile> ReadAsync(Stream stream, bool decompressCels, CancellationToken cancellationToken = default) =>
		AsepriteFileReader.ReadAsync(stream, decompressCels, cancellationToken);

	public static ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> memory) => AsepriteCompressionReader.Decompress(memory);
	public static int DecompressInto(ReadOnlyMemory<byte> input, byte[] output, int offset, int length) => AsepriteCompressionReader.DecompressInto(input, output, offset, length);
}
