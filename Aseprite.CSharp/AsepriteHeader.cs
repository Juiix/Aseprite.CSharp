namespace Aseprite.CSharp;

public sealed record AsepriteHeader
(
	uint FileSize,
	ushort Magic,          // must be 0xA5E0
	ushort Frames,
	ushort Width,
	ushort Height,
	ColorDepth Depth,
	AsepriteFileFlags Flags,
	ushort Speed,          // deprecated
	uint Reserved1,      // always 0
	uint Reserved2,      // always 0
	byte TransparentIndex,
	ushort ColorCount,     // 0 → 256 for old files
	byte PixelWidth,
	byte PixelHeight,
	short GridX,
	short GridY,
	ushort GridWidth,
	ushort GridHeight
);
