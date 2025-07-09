namespace Aseprite.CSharp;

public readonly record struct PaletteEntry(Rgba Color, string? Name);

internal sealed record AsepritePalette
(
	uint NewSize,    // full palette length after applying this chunk
	uint FromIndex,  // first index modified
	uint ToIndex,    // last  index modified  (inclusive)
	PaletteEntry[] Entries    // length = ToIndex-FromIndex+1
) : AsepriteObject;