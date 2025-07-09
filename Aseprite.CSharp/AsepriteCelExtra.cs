namespace Aseprite.CSharp;

public sealed record AsepriteCelExtra
(
	CelExtraFlags Flags,
	float PreciseX,
	float PreciseY,
	float Width,
	float Height
) : AsepriteObject;