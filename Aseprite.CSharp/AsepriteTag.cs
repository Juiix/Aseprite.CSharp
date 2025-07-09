namespace Aseprite.CSharp;

public sealed record AsepriteTag
(
	ushort FromFrame,
	ushort ToFrame,
	TagDirection Direction,
	ushort Repeat,        // 0 -> "unspecified"
	Rgba Color,         // deprecated RGB (alpha = 255)
	string Name
) : AsepriteObject;