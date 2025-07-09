namespace Aseprite.CSharp;

public sealed record AsepriteCel
(
	ushort LayerIndex,
	short X,
	short Y,
	byte Opacity,
	CelType Type,
	short ZIndex,
	CelContent Content,
	AsepriteCelExtra? Extra
) : AsepriteObject;

public abstract record CelContent;

public sealed record RawCelContent
(ushort Width, ushort Height, ReadOnlyMemory<byte> Pixels) : CelContent;

public sealed record LinkedCelContent
(ushort LinkedFrameIndex) : CelContent;

public sealed record TilemapCelContent
(
	ushort Width, ushort Height, ushort BitsPerTile,
	uint TileIdMask, uint XFlipMask, uint YFlipMask, uint DiagFlipMask,
	ReadOnlyMemory<byte> Tiles
) : CelContent;

public sealed record CompressedImageCelContent
(ushort Width, ushort Height, ReadOnlyMemory<byte> Compressed) : CelContent;

public sealed record CompressedTilemapCelContent
(
	ushort Width, ushort Height, ushort BitsPerTile,
	uint TileIdMask, uint XFlipMask, uint YFlipMask, uint DiagFlipMask,
	ReadOnlyMemory<byte> Compressed
) : CelContent;